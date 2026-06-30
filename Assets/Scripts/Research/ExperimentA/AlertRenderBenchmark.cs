using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhishingVR.Research.ExperimentA
{
    /// <summary>
    /// EXPERIMENTO A — Renderizado concurrente de alertas holográficas.
    ///
    /// Barre una lista de cantidades N de alertas y, para cada N, mide el frametime
    /// durante unos segundos. Repite todo el barrido para cada técnica de optimización
    /// (Baseline vs. GPU Instancing) de modo que un único CSV contiene la comparación
    /// con/sin técnica que necesita el paper.
    ///
    /// Salida (Application.persistentDataPath/benchmarks/):
    ///   - expA_render_summary_*.csv : una fila por (técnica, N) con media/mediana/p95/p99.
    ///   - expA_render_raw_*.csv     : (opcional) una fila por frame, para distribuciones.
    ///
    /// Frametime: usa FrameTimingManager (CPU/GPU ms) si está disponible
    /// — requiere "Frame Timing Stats" activado en Player Settings — y SIEMPRE
    /// registra además el wall-clock por frame (Time.unscaledDeltaTime).
    ///
    /// NOTA: en modo passthrough no se puede detectar por código de forma fiable;
    /// marca <see cref="passthroughEnabled"/> a mano según cómo lances la corrida,
    /// para que quede estampado en el CSV.
    /// </summary>
    public sealed class AlertRenderBenchmark : MonoBehaviour
    {
        public enum Technique { Baseline, GpuInstancing }

        [Header("Barrido")]
        [Tooltip("Cantidades de alertas a evaluar, en orden.")]
        [SerializeField] private int[] alertCounts = { 1, 5, 10, 25, 50, 100 };

        [Tooltip("Segundos de muestreo por cada paso (tras el warm-up).")]
        [SerializeField] private float secondsPerStep = 4f;

        [Tooltip("Segundos de descarte antes de medir cada paso (estabiliza el pipeline).")]
        [SerializeField] private float warmupSeconds = 1f;

        [Tooltip("Si está activo, corre el barrido para Baseline Y para GPU Instancing.")]
        [SerializeField] private bool sweepBothTechniques = true;

        [Tooltip("Técnica usada si sweepBothTechniques está desactivado.")]
        [SerializeField] private Technique singleTechnique = Technique.Baseline;

        [Header("Layout de las alertas")]
        [Tooltip("Centro del campo de alertas. Si es null, se usa este transform.")]
        [SerializeField] private Transform spawnOrigin;
        [SerializeField] private int columns = 10;
        [SerializeField] private float spacing = 0.35f;
        [SerializeField] private Vector3 alertScale = new Vector3(0.18f, 0.18f, 0.18f);

        [Header("Salida")]
        [SerializeField] private bool writeRawPerFrame = true;
        [Tooltip("Solo se estampa en el CSV; no cambia el render. Márcalo según tu corrida.")]
        [SerializeField] private bool passthroughEnabled = false;

        [Header("Ejecución")]
        [Tooltip("Arranca el barrido automáticamente en Start.")]
        [SerializeField] private bool runOnStart = true;

        private Transform _container;
        private Material _sharedMat;
        private readonly List<GameObject> _alerts = new();
        private FrameTiming[] _frameTimings = new FrameTiming[1];
        private bool _running;

        private void Start()
        {
            if (runOnStart) StartBenchmark();
        }

        /// <summary>Punto de entrada público (botón, tecla o Start).</summary>
        public void StartBenchmark()
        {
            if (_running) return;
            StartCoroutine(RunAll());
        }

        private IEnumerator RunAll()
        {
            _running = true;

            string stampHeaderSummary =
                "technique,target_n,actual_n,sample_frames,mean_ms,median_ms,p95_ms,p99_ms," +
                "min_ms,max_ms,stddev_ms,mean_fps,gpu_mean_ms,passthrough";
            string stampHeaderRaw =
                "technique,target_n,frame_index,frametime_ms,gpu_ms";

            using var summary = new BenchmarkCsvLogger("expA_render_summary", stampHeaderSummary);
            using var raw = writeRawPerFrame
                ? new BenchmarkCsvLogger("expA_render_raw", stampHeaderRaw)
                : null;

            EnsureContainer();

            var techniques = sweepBothTechniques
                ? new[] { Technique.Baseline, Technique.GpuInstancing }
                : new[] { singleTechnique };

            foreach (var tech in techniques)
            {
                foreach (int n in alertCounts)
                {
                    BuildField(n, tech == Technique.GpuInstancing);

                    // Warm-up: dejamos correr sin medir.
                    float t = 0f;
                    while (t < warmupSeconds) { t += Time.unscaledDeltaTime; yield return null; }

                    // Muestreo.
                    var frametimes = new List<double>(512);
                    var gpuTimes = new List<double>(512);
                    int frameIndex = 0;
                    t = 0f;
                    while (t < secondsPerStep)
                    {
                        float dtMs = Time.unscaledDeltaTime * 1000f;
                        frametimes.Add(dtMs);

                        double gpuMs = double.NaN;
                        if (TryGetGpuMs(out double gpu)) { gpuMs = gpu; gpuTimes.Add(gpu); }

                        raw?.WriteRow(tech.ToString(), n, frameIndex, dtMs,
                                      double.IsNaN(gpuMs) ? "" : gpuMs.ToString("R", System.Globalization.CultureInfo.InvariantCulture));

                        frameIndex++;
                        t += Time.unscaledDeltaTime;
                        yield return null;
                    }

                    var s = SampleStats.Compute(frametimes);
                    var g = SampleStats.Compute(gpuTimes);
                    double meanFps = s.Mean > 0d ? 1000d / s.Mean : 0d;

                    summary.WriteRow(
                        tech.ToString(), n, _alerts.Count, s.Count,
                        s.Mean, s.Median, s.P95, s.P99, s.Min, s.Max, s.StdDev,
                        meanFps, g.Count > 0 ? g.Mean : 0d, passthroughEnabled);
                    summary.Flush();

                    Debug.Log($"[ExpA] {tech} N={n}: media {s.Mean:F2} ms " +
                              $"(p95 {s.P95:F2}) · {meanFps:F0} FPS · {s.Count} frames");
                }
            }

            ClearField();
            _running = false;
            Debug.Log($"[ExpA] Barrido completo. CSV resumen → {summary.FilePath}");
        }

        // ── Construcción del campo de alertas ────────────────────────────────

        private void EnsureContainer()
        {
            if (_container != null) return;
            _container = new GameObject("AlertField").transform;
            _container.SetParent(transform, false);
        }

        private void BuildField(int n, bool instancing)
        {
            ClearField();

            // Material compartido: un solo material para que el instancing pueda batched-render.
            if (_sharedMat == null) _sharedMat = CreateAlertMaterial();
            _sharedMat.enableInstancing = instancing;

            Transform origin = spawnOrigin != null ? spawnOrigin : transform;
            int cols = Mathf.Max(1, columns);

            for (int i = 0; i < n; i++)
            {
                int row = i / cols;
                int col = i % cols;

                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Alert_{i:D3}";

                // Quitamos el collider: el Exp. A solo mide render, no interacción.
                var col3d = go.GetComponent<Collider>();
                if (col3d != null) Destroy(col3d);

                go.transform.SetParent(_container, false);
                go.transform.localScale = alertScale;

                Vector3 offset = new Vector3(
                    (col - (cols - 1) * 0.5f) * spacing,
                    -row * spacing,
                    0f);
                go.transform.position = origin.position + origin.rotation * offset;

                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = _sharedMat;

                go.AddComponent<HolographicAlert>();
                _alerts.Add(go);
            }
        }

        private void ClearField()
        {
            for (int i = 0; i < _alerts.Count; i++)
                if (_alerts[i] != null) Destroy(_alerts[i]);
            _alerts.Clear();
        }

        private static Material CreateAlertMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);

            var cyan = new Color(0.1f, 0.8f, 1f, 1f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", cyan);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", cyan);
            return mat;
        }

        // ── Frametime GPU vía FrameTimingManager ─────────────────────────────

        private bool TryGetGpuMs(out double gpuMs)
        {
            gpuMs = 0d;
            FrameTimingManager.CaptureFrameTimings();
            uint got = FrameTimingManager.GetLatestTimings(1, _frameTimings);
            if (got < 1) return false;

            gpuMs = _frameTimings[0].gpuFrameTime; // ms; 0 si el driver no lo reporta
            return gpuMs > 0d;
        }

        private void OnDestroy()
        {
            if (_sharedMat != null) Destroy(_sharedMat);
        }
    }
}
