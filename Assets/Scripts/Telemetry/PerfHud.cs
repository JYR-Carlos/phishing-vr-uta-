using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PhishVR.Telemetry
{
    /// <summary>
    /// HUD World Space anclado a la cámara XR.
    /// Muestra: condición activa, FPS, CPU/GPU ms, frames perdidos, latencia de mano.
    /// Toggle on/off con el botón assignado (o llamando Toggle() desde código).
    /// No genera GC por frame: reutiliza un StringBuilder pre-allocado.
    /// </summary>
    public sealed class PerfHud : MonoBehaviour
    {
        [Header("Referencias")]
        public PerfSampler       sampler;
        public HandLatencyProbe  handProbe;
        public ExperimentRunner  runner;

        [Header("UI")]
        public Text hudText;   // Text de UGUI del canvas World Space

        [Header("Toggle input")]
        public InputActionReference toggleAction;

        [Header("Actualización")]
        [Tooltip("Intervalo de refresco del texto (segundos)")]
        [Range(0.05f, 1f)]
        public float updateInterval = 0.1f;

        private bool          _visible = true;
        private float         _nextUpdate;
        private readonly StringBuilder _sb = new StringBuilder(256);

        void OnEnable()
        {
            if (toggleAction != null)
            {
                toggleAction.action.Enable();
                toggleAction.action.performed += OnToggle;
            }
        }

        void OnDisable()
        {
            if (toggleAction != null)
                toggleAction.action.performed -= OnToggle;
        }

        void OnToggle(InputAction.CallbackContext _) => Toggle();

        public void Toggle()
        {
            _visible = !_visible;
            if (hudText != null) hudText.gameObject.SetActive(_visible);
        }

        void Update()
        {
            if (!_visible || hudText == null || sampler == null) return;
            if (Time.realtimeSinceStartup < _nextUpdate) return;
            _nextUpdate = Time.realtimeSinceStartup + updateInterval;

            _sb.Clear();

            // Condición
            string condId   = sampler.CurrentConditionId ?? "—";
            bool   inWarmup = runner != null && runner.InWarmup;
            float  progress = runner != null ? runner.CurrentProgress : 0f;

            _sb.Append("<b>"); _sb.Append(condId); _sb.Append("</b>");
            if (inWarmup) _sb.Append("  [WARMUP]");
            else
            {
                _sb.Append("  ");
                AppendBar(_sb, progress, 10);
            }
            _sb.AppendLine();

            // FPS
            _sb.Append("FPS   "); AppendF1(_sb, sampler.LatestFps);   _sb.AppendLine();

            // CPU / GPU
            _sb.Append("CPU   "); AppendF2(_sb, sampler.LatestCpuMs); _sb.Append(" ms"); _sb.AppendLine();
            _sb.Append("GPU   "); AppendF2(_sb, sampler.LatestGpuMs); _sb.Append(" ms"); _sb.AppendLine();

            // Dropped
            _sb.Append("Drop  "); _sb.Append(sampler.LatestDroppedDelta); _sb.Append(" frm"); _sb.AppendLine();

            // Hand latency
            float hand = handProbe != null ? handProbe.HandTrackingLatencyMs : 0f;
            _sb.Append("Hand  "); AppendF1(_sb, hand); _sb.Append(" ms");

            hudText.text = _sb.ToString();
        }

        private static void AppendF1(StringBuilder sb, float v) =>
            sb.Append(v.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));

        private static void AppendF2(StringBuilder sb, float v) =>
            sb.Append(v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));

        private static void AppendBar(StringBuilder sb, float t, int width)
        {
            int filled = Mathf.RoundToInt(t * width);
            sb.Append('[');
            for (int i = 0; i < width; i++)
                sb.Append(i < filled ? '#' : '.');
            sb.Append(']');
        }
    }
}
