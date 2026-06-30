using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PhishVR.Telemetry
{
    /// <summary>
    /// Orquesta el experimento A/B: ejecuta cada RenderCondition durante una duración fija,
    /// descarta el período de warm-up, y acumula estadísticas en línea (Welford + reservoir)
    /// para que la duración de medición no esté limitada por el tamaño del ring buffer.
    /// </summary>
    [RequireComponent(typeof(PerfSampler))]
    [RequireComponent(typeof(RenderConditionController))]
    [RequireComponent(typeof(CsvLogger))]
    public sealed class ExperimentRunner : MonoBehaviour
    {
        [Header("Condiciones")]
        public RenderCondition[] conditions;

        [Header("Tiempos (segundos)")]
        [Tooltip("Duración total de medición por condición (sin warm-up)")]
        public float measureDurationSec = 60f;
        [Tooltip("Período de warm-up descartado al inicio de cada condición")]
        public float warmupDurationSec  = 5f;

        [Header("Avance")]
        [Tooltip("Si está marcado, avanza automáticamente. Si no, usa el botón assignado.")]
        public bool autoAdvance = true;
        [Tooltip("InputAction para avanzar manualmente (ej: Primary Button / Button South)")]
        public InputActionReference advanceAction;

        public bool  IsRunning       { get; private set; }
        public int   CurrentIndex    { get; private set; }
        public float CurrentProgress { get; private set; }
        public bool  InWarmup        { get; private set; }

        public event Action<ConditionSummary> OnConditionComplete;
        public event Action<List<ConditionSummary>> OnExperimentComplete;

        // ── Resumen por condición ──────────────────────────────────────────
        public struct ConditionSummary
        {
            public string ConditionId;
            public string DisplayName;
            public float  MeanFps;
            public float  MeanCpuMs;
            public float  MeanGpuMs;
            public float  P95CpuMs;
            public float  P99CpuMs;
            public float  StdDevCpuMs;
            public float  P95GpuMs;
            public float  P99GpuMs;
            public float  StdDevGpuMs;
            public float  DroppedFramesPct;
            public float  MeanHandLatencyMs;
            public int    SampleCount;
        }

        // ── Acumuladores en línea (Welford) — sin GC, sin límite de duración ──
        private struct WelfordAcc
        {
            public int    n;
            public double mean;
            public double M2;   // varianza acumulada (algoritmo de Welford)

            public void Add(float x)
            {
                n++;
                double delta  = x - mean;
                mean += delta / n;
                double delta2 = x - mean;
                M2   += delta * delta2;
            }

            public float Mean   => (float)mean;
            public float StdDev => n > 1 ? (float)Math.Sqrt(M2 / n) : 0f;
        }

        // Reservoir sampling para percentiles (tamaño fijo, no GC durante steady-state)
        private const int ReservoirSize = 2000;
        private float[] _resCpu  = new float[ReservoirSize];
        private float[] _resGpu  = new float[ReservoirSize];
        private float[] _resSorted = new float[ReservoirSize];
        private int     _resCount;
        private uint    _rngState = 12345;  // LCG seed

        private PerfSampler               _sampler;
        private RenderConditionController _controller;
        private CsvLogger                 _logger;
        private bool                      _advanceRequested;
        private int                       _lastFrameIndex;

        void Awake()
        {
            _sampler    = GetComponent<PerfSampler>();
            _controller = GetComponent<RenderConditionController>();
            _logger     = GetComponent<CsvLogger>();
        }

        void OnEnable()
        {
            if (advanceAction != null)
            {
                advanceAction.action.Enable();
                advanceAction.action.performed += OnAdvanceInput;
            }
        }

        void OnDisable()
        {
            if (advanceAction != null)
                advanceAction.action.performed -= OnAdvanceInput;
        }

        void OnAdvanceInput(InputAction.CallbackContext _) => _advanceRequested = true;

        public void StartExperiment()
        {
            if (IsRunning) return;
            if (conditions == null || conditions.Length == 0)
            {
                Debug.LogError("[ExperimentRunner] No hay condiciones configuradas.");
                return;
            }
            StartCoroutine(RunExperiment());
        }

        private IEnumerator RunExperiment()
        {
            IsRunning    = true;
            CurrentIndex = 0;
            var summaries = new List<ConditionSummary>(conditions.Length);

            _logger.OpenSession();

            foreach (var condition in conditions)
            {
                if (condition == null) { CurrentIndex++; continue; }

                Debug.Log($"[ExperimentRunner] Condición {CurrentIndex + 1}/{conditions.Length}: {condition.displayName}");
                _controller.Apply(condition);

                // ── Warm-up ───────────────────────────────────────────────
                InWarmup = true;
                float elapsed = 0f;
                while (elapsed < warmupDurationSec)
                {
                    if (!autoAdvance && _advanceRequested) { _advanceRequested = false; break; }
                    elapsed += Time.unscaledDeltaTime;
                    CurrentProgress = elapsed / warmupDurationSec;
                    yield return null;
                }

                // Descartar frames del warmup del raw CSV y de los acumuladores
                _logger.SkipToCurrentFrame();
                ResetAccumulators();
                // TotalFrames-1 para que el primer frame de medición (FrameIndex == TotalFrames)
                // pase el guard ">" en AccumulateCurrentFrame
                _lastFrameIndex = _sampler.TotalFrames - 1;

                // ── Medición ──────────────────────────────────────────────
                InWarmup = false;
                elapsed  = 0f;

                while (elapsed < measureDurationSec)
                {
                    if (!autoAdvance && _advanceRequested) { _advanceRequested = false; break; }
                    elapsed += Time.unscaledDeltaTime;
                    CurrentProgress = elapsed / measureDurationSec;

                    // Acumular estadísticas del frame actual (GC-free)
                    AccumulateCurrentFrame();

                    // Volcar frame al CSV (periódicamente dentro de CsvLogger)
                    _logger.FlushPending();
                    yield return null;
                }

                var summary = BuildSummary(condition);
                summaries.Add(summary);
                _logger.WriteSummaryRow(summary);
                OnConditionComplete?.Invoke(summary);

                Debug.Log($"[ExperimentRunner] {condition.displayName}: " +
                          $"fps={summary.MeanFps:F1} cpu={summary.MeanCpuMs:F2}ms gpu={summary.MeanGpuMs:F2}ms " +
                          $"p95cpu={summary.P95CpuMs:F2}ms dropped={summary.DroppedFramesPct:F1}% " +
                          $"hand={summary.MeanHandLatencyMs:F1}ms n={summary.SampleCount}");

                CurrentIndex++;
            }

            _logger.CloseSession();
            _controller.RestoreDefaults();
            IsRunning = false;
            OnExperimentComplete?.Invoke(summaries);
        }

        // ── Acumuladores Welford (campos directos para evitar boxing) ──────
        private WelfordAcc _accCpu, _accGpu, _accFps, _accHand;
        private int        _droppedTotal;

        private void ResetAccumulators()
        {
            _accCpu   = default;
            _accGpu   = default;
            _accFps   = default;
            _accHand  = default;
            _droppedTotal = 0;
            _resCount = 0;
            _rngState = 12345;
        }

        private void AccumulateCurrentFrame()
        {
            // Leer el último frame escrito al ring buffer (evita copiar struct)
            int lastIdx = (_sampler.WriteHead - 1 + PerfSampler.RingBufferSize) % PerfSampler.RingBufferSize;
            ref var s = ref _sampler.Buffer[lastIdx];

            // Solo acumular si es un frame nuevo
            if (s.FrameIndex <= _lastFrameIndex) return;
            _lastFrameIndex = s.FrameIndex;

            _accCpu.Add(s.CpuFrameMs);
            _accGpu.Add(s.GpuFrameMs);
            _accFps.Add(s.Fps);
            _accHand.Add(s.HandLatencyMs);
            _droppedTotal += s.DroppedFramesDelta;

            // Reservoir sampling (Vitter's Algorithm R) para percentiles — GC-free
            int n = _accCpu.n;
            if (n <= ReservoirSize)
            {
                _resCpu[n - 1] = s.CpuFrameMs;
                _resGpu[n - 1] = s.GpuFrameMs;
                _resCount = n;
            }
            else
            {
                uint r = NextRng() % (uint)n;
                if (r < ReservoirSize)
                {
                    _resCpu[(int)r] = s.CpuFrameMs;
                    _resGpu[(int)r] = s.GpuFrameMs;
                }
            }
        }

        private ConditionSummary BuildSummary(RenderCondition condition)
        {
            int n = _accCpu.n;
            if (n == 0) return new ConditionSummary { ConditionId = condition.conditionId };

            float p95Cpu, p99Cpu, p95Gpu, p99Gpu;
            ComputePercentiles(_resCpu, _resCount, out p95Cpu, out p99Cpu);
            ComputePercentiles(_resGpu, _resCount, out p95Gpu, out p99Gpu);

            return new ConditionSummary
            {
                ConditionId       = condition.conditionId,
                DisplayName       = condition.displayName,
                MeanFps           = _accFps.Mean,
                MeanCpuMs         = _accCpu.Mean,
                MeanGpuMs         = _accGpu.Mean,
                P95CpuMs          = p95Cpu,
                P99CpuMs          = p99Cpu,
                StdDevCpuMs       = _accCpu.StdDev,
                P95GpuMs          = p95Gpu,
                P99GpuMs          = p99Gpu,
                StdDevGpuMs       = _accGpu.StdDev,
                DroppedFramesPct  = n > 0 ? (_droppedTotal / (float)n) * 100f : 0f,
                MeanHandLatencyMs = _accHand.Mean,
                SampleCount       = n
            };
        }

        private void ComputePercentiles(float[] reservoir, int count, out float p95, out float p99)
        {
            if (count == 0) { p95 = 0f; p99 = 0f; return; }
            Array.Copy(reservoir, _resSorted, count);
            Array.Sort(_resSorted, 0, count);
            p95 = _resSorted[Mathf.Clamp((int)(0.95f * count), 0, count - 1)];
            p99 = _resSorted[Mathf.Clamp((int)(0.99f * count), 0, count - 1)];
        }

        // LCG sin alloc para reservoir sampling
        private uint NextRng()
        {
            _rngState = _rngState * 1664525u + 1013904223u;
            return _rngState;
        }
    }
}
