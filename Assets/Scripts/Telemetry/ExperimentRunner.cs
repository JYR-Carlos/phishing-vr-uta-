using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PhishVR.Telemetry
{
    /// <summary>
    /// Orquesta el experimento A/B: ejecuta cada RenderCondition durante una duración fija,
    /// descarta el período de warm-up y guarda estadísticas al finalizar cada condición.
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

        public bool  IsRunning        { get; private set; }
        public int   CurrentIndex     { get; private set; }
        public float CurrentProgress  { get; private set; }  // 0-1 dentro de la condición activa
        public bool  InWarmup         { get; private set; }

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

        // ── Private ────────────────────────────────────────────────────────
        private PerfSampler              _sampler;
        private RenderConditionController _controller;
        private CsvLogger                _logger;
        private bool                     _advanceRequested;

        // Buffers pre-allocated para cálculo de estadísticas (sin GC en steady state)
        private float[] _statBufCpu = new float[PerfSampler.RingBufferSize];
        private float[] _statBufGpu = new float[PerfSampler.RingBufferSize];
        private float[] _statBufFps = new float[PerfSampler.RingBufferSize];

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
            IsRunning     = true;
            CurrentIndex  = 0;
            var summaries = new List<ConditionSummary>(conditions.Length);

            _logger.OpenSession();

            foreach (var condition in conditions)
            {
                if (condition == null) { CurrentIndex++; continue; }

                Debug.Log($"[ExperimentRunner] Iniciando condición {CurrentIndex + 1}/{conditions.Length}: {condition.displayName}");
                _controller.Apply(condition);

                // Warm-up: captura datos pero no los usa
                InWarmup = true;
                float elapsed = 0f;
                while (elapsed < warmupDurationSec)
                {
                    if (!autoAdvance && _advanceRequested) { _advanceRequested = false; break; }
                    elapsed += Time.unscaledDeltaTime;
                    CurrentProgress = elapsed / warmupDurationSec;
                    yield return null;
                }

                // Measurement: snapshot del ring buffer al inicio, luego al final
                InWarmup = false;
                int frameStart = _sampler.TotalFrames;
                elapsed = 0f;

                while (elapsed < measureDurationSec)
                {
                    if (!autoAdvance && _advanceRequested) { _advanceRequested = false; break; }
                    elapsed += Time.unscaledDeltaTime;
                    CurrentProgress = elapsed / measureDurationSec;

                    // Volcar frames al CSV en tiempo real
                    _logger.FlushPending();
                    yield return null;
                }

                // Calcular estadísticas sobre el período medido
                int frameEnd  = _sampler.TotalFrames;
                var summary   = ComputeSummary(condition, frameStart, frameEnd);
                summaries.Add(summary);
                _logger.WriteSummaryRow(summary);
                OnConditionComplete?.Invoke(summary);

                Debug.Log($"[ExperimentRunner] Condición {condition.displayName} completada: " +
                          $"fps={summary.MeanFps:F1} cpu={summary.MeanCpuMs:F2}ms gpu={summary.MeanGpuMs:F2}ms " +
                          $"dropped={summary.DroppedFramesPct:F1}% hand_lat={summary.MeanHandLatencyMs:F1}ms");

                CurrentIndex++;
            }

            _logger.CloseSession();
            _controller.RestoreDefaults();
            IsRunning = false;
            OnExperimentComplete?.Invoke(summaries);
            Debug.Log("[ExperimentRunner] Experimento completado. Extrae los CSV con: adb pull <ruta>");
        }

        private ConditionSummary ComputeSummary(RenderCondition condition, int frameStart, int frameEnd)
        {
            int count = 0;
            double sumFps = 0, sumCpu = 0, sumGpu = 0, sumHand = 0;
            int totalDropped = 0;

            // Recorre el ring buffer en el rango medido
            int totalFrames = _sampler.TotalFrames;
            int ringSize    = PerfSampler.RingBufferSize;

            for (int fi = frameStart; fi < frameEnd; fi++)
            {
                int idx = fi % ringSize;
                // Guardia: si el buffer dio vuelta y sobreescribió, saltamos
                if (totalFrames - fi > ringSize) continue;

                ref var s = ref _sampler.Buffer[idx];
                if (count < _statBufCpu.Length)
                {
                    _statBufCpu[count] = s.CpuFrameMs;
                    _statBufGpu[count] = s.GpuFrameMs;
                    _statBufFps[count] = s.Fps;
                }
                sumFps  += s.Fps;
                sumCpu  += s.CpuFrameMs;
                sumGpu  += s.GpuFrameMs;
                sumHand += s.HandLatencyMs;
                totalDropped += s.DroppedFramesDelta;
                count++;
            }

            if (count == 0) return new ConditionSummary { ConditionId = condition.conditionId };

            float meanFps  = (float)(sumFps  / count);
            float meanCpu  = (float)(sumCpu  / count);
            float meanGpu  = (float)(sumGpu  / count);
            float meanHand = (float)(sumHand / count);

            int statCount = Math.Min(count, _statBufCpu.Length);
            Array.Sort(_statBufCpu, 0, statCount);
            Array.Sort(_statBufGpu, 0, statCount);

            float p95Cpu = Percentile(_statBufCpu, statCount, 0.95f);
            float p99Cpu = Percentile(_statBufCpu, statCount, 0.99f);
            float p95Gpu = Percentile(_statBufGpu, statCount, 0.95f);
            float p99Gpu = Percentile(_statBufGpu, statCount, 0.99f);
            float stdCpu = StdDev(_statBufCpu, statCount, meanCpu);
            float stdGpu = StdDev(_statBufGpu, statCount, meanGpu);

            float droppedPct = count > 0 ? (totalDropped / (float)count) * 100f : 0f;

            return new ConditionSummary
            {
                ConditionId      = condition.conditionId,
                DisplayName      = condition.displayName,
                MeanFps          = meanFps,
                MeanCpuMs        = meanCpu,
                MeanGpuMs        = meanGpu,
                P95CpuMs         = p95Cpu,
                P99CpuMs         = p99Cpu,
                StdDevCpuMs      = stdCpu,
                P95GpuMs         = p95Gpu,
                P99GpuMs         = p99Gpu,
                StdDevGpuMs      = stdGpu,
                DroppedFramesPct = droppedPct,
                MeanHandLatencyMs = meanHand,
                SampleCount      = count
            };
        }

        private static float Percentile(float[] sorted, int count, float p)
        {
            if (count == 0) return 0f;
            int idx = Mathf.Clamp((int)(p * count), 0, count - 1);
            return sorted[idx];
        }

        private static float StdDev(float[] arr, int count, float mean)
        {
            if (count == 0) return 0f;
            double variance = 0;
            for (int i = 0; i < count; i++)
            {
                double d = arr[i] - mean;
                variance += d * d;
            }
            return (float)Math.Sqrt(variance / count);
        }
    }
}
