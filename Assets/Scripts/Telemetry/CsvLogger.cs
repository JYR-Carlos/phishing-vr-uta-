using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace PhishVR.Telemetry
{
    /// <summary>
    /// Escribe dos CSV por sesión en Application.persistentDataPath:
    ///   raw_YYYYMMDD_HHmmss.csv    — una fila por frame
    ///   summary_YYYYMMDD_HHmmss.csv — una fila por condición (agregados)
    ///
    /// El flush periódico (cada flushIntervalSec) protege contra crashes.
    /// No hace allocs de heap durante la escritura en estado estable.
    /// </summary>
    [RequireComponent(typeof(PerfSampler))]
    public sealed class CsvLogger : MonoBehaviour
    {
        [Tooltip("Segundos entre flushes a disco (reduce riesgo de pérdida por crash)")]
        [Range(1f, 30f)]
        public float flushIntervalSec = 5f;

        // ── Public API ─────────────────────────────────────────────────────
        public string RawFilePath     { get; private set; }
        public string SummaryFilePath { get; private set; }

        // ── Private ────────────────────────────────────────────────────────
        private PerfSampler  _sampler;
        private StreamWriter _rawWriter;
        private StreamWriter _summaryWriter;
        private int          _lastFlushedFrame;
        private float        _lastFlushTime;

        // Pre-allocated StringBuilder para evitar allocs en FormatRow
        private readonly StringBuilder _sb = new StringBuilder(256);

        private static readonly string RawHeader =
            "timestamp_iso,condition_id,frame_index,fps,cpu_ms,gpu_ms," +
            "dropped_frames,hand_latency_ms,ffr_level,render_scale,msaa";

        private static readonly string SummaryHeader =
            "condition_id,display_name,sample_count,mean_fps,mean_cpu_ms,p95_cpu_ms," +
            "p99_cpu_ms,stddev_cpu_ms,mean_gpu_ms,p95_gpu_ms,p99_gpu_ms,stddev_gpu_ms," +
            "dropped_frames_pct,mean_hand_latency_ms";

        void Awake()
        {
            _sampler = GetComponent<PerfSampler>();
        }

        public void OpenSession()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string dir       = Application.persistentDataPath;

            RawFilePath     = Path.Combine(dir, $"raw_{timestamp}.csv");
            SummaryFilePath = Path.Combine(dir, $"summary_{timestamp}.csv");

            _rawWriter     = new StreamWriter(RawFilePath,     append: false, encoding: Encoding.UTF8) { AutoFlush = false };
            _summaryWriter = new StreamWriter(SummaryFilePath, append: false, encoding: Encoding.UTF8) { AutoFlush = false };

            _rawWriter.WriteLine(RawHeader);
            _summaryWriter.WriteLine(SummaryHeader);
            _rawWriter.Flush();
            _summaryWriter.Flush();

            _lastFlushedFrame = _sampler.TotalFrames;
            _lastFlushTime    = Time.realtimeSinceStartup;

            Debug.Log($"[CsvLogger] Archivos de sesión:\n  RAW:     {RawFilePath}\n  SUMMARY: {SummaryFilePath}");
            Debug.Log($"[CsvLogger] Para extraer: adb pull \"{RawFilePath}\" && adb pull \"{SummaryFilePath}\"");
        }

        /// <summary>
        /// Vuelca al disco las filas pendientes del ring buffer.
        /// Llamado por ExperimentRunner en cada frame durante la medición.
        /// </summary>
        public void FlushPending()
        {
            if (_rawWriter == null) return;

            int ringSize = PerfSampler.RingBufferSize;
            int current  = _sampler.TotalFrames;

            for (int fi = _lastFlushedFrame; fi < current; fi++)
            {
                // Guardia: buffer dio vuelta
                if (current - fi > ringSize) { _lastFlushedFrame = fi + 1; continue; }
                ref var s = ref _sampler.Buffer[fi % ringSize];
                WriteRawRow(in s);
            }
            _lastFlushedFrame = current;

            // Flush periódico a disco
            float now = Time.realtimeSinceStartup;
            if (now - _lastFlushTime >= flushIntervalSec)
            {
                _rawWriter.Flush();
                _lastFlushTime = now;
            }
        }

        public void WriteSummaryRow(in ExperimentRunner.ConditionSummary s)
        {
            if (_summaryWriter == null) return;

            _sb.Clear();
            _sb.Append(Escape(s.ConditionId)); _sb.Append(',');
            _sb.Append(Escape(s.DisplayName));  _sb.Append(',');
            _sb.Append(s.SampleCount);           _sb.Append(',');
            AppendF2(_sb, s.MeanFps);            _sb.Append(',');
            AppendF3(_sb, s.MeanCpuMs);          _sb.Append(',');
            AppendF3(_sb, s.P95CpuMs);           _sb.Append(',');
            AppendF3(_sb, s.P99CpuMs);           _sb.Append(',');
            AppendF3(_sb, s.StdDevCpuMs);        _sb.Append(',');
            AppendF3(_sb, s.MeanGpuMs);          _sb.Append(',');
            AppendF3(_sb, s.P95GpuMs);           _sb.Append(',');
            AppendF3(_sb, s.P99GpuMs);           _sb.Append(',');
            AppendF3(_sb, s.StdDevGpuMs);        _sb.Append(',');
            AppendF2(_sb, s.DroppedFramesPct);   _sb.Append(',');
            AppendF3(_sb, s.MeanHandLatencyMs);

            _summaryWriter.WriteLine(_sb);
            _summaryWriter.Flush();
        }

        /// <summary>
        /// Avanza el puntero interno para ignorar frames del warm-up en el raw CSV.
        /// Llamar justo antes del período de medición.
        /// </summary>
        public void SkipToCurrentFrame()
        {
            _lastFlushedFrame = _sampler.TotalFrames;
        }

        public void CloseSession()
        {
            FlushPending();
            _rawWriter?.Flush();
            _rawWriter?.Close();
            _summaryWriter?.Flush();
            _summaryWriter?.Close();
            _rawWriter     = null;
            _summaryWriter = null;

            Debug.Log($"[CsvLogger] Sesión cerrada. Extrae con:\n" +
                      $"  adb pull \"{Application.persistentDataPath}\"");
        }

        void OnDestroy() => CloseSession();

        // ── Helpers (no alloc en steady state con StringBuilder re-usado) ──
        private void WriteRawRow(in PerfSampler.FrameSample s)
        {
            _sb.Clear();
            // ISO timestamp desde epoch
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                              .AddSeconds(s.TimestampSec);
            _sb.Append(dt.ToString("yyyy-MM-ddTHH:mm:ss.fff")); _sb.Append(',');
            _sb.Append(Escape(s.ConditionId));   _sb.Append(',');
            _sb.Append(s.FrameIndex);             _sb.Append(',');
            AppendF2(_sb, s.Fps);                 _sb.Append(',');
            AppendF3(_sb, s.CpuFrameMs);          _sb.Append(',');
            AppendF3(_sb, s.GpuFrameMs);          _sb.Append(',');
            _sb.Append(s.DroppedFramesDelta);     _sb.Append(',');
            AppendF3(_sb, s.HandLatencyMs);        _sb.Append(',');
            _sb.Append(s.FfrLevel);               _sb.Append(',');
            AppendF3(_sb, s.RenderScale);          _sb.Append(',');
            _sb.Append(s.MsaaSampleCount);
            _rawWriter.WriteLine(_sb);
        }

        private static void AppendF2(StringBuilder sb, float v) =>
            sb.Append(v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));

        private static void AppendF3(StringBuilder sb, float v) =>
            sb.Append(v.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));

        private static string Escape(string v)
        {
            if (v == null) return string.Empty;
            if (v.IndexOf(',') >= 0 || v.IndexOf('"') >= 0)
                return "\"" + v.Replace("\"", "\"\"") + "\"";
            return v;
        }
    }
}
