using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace PhishVR.Telemetry
{
    /// <summary>
    /// Proxy de latencia de hand tracking usando XRDisplaySubsystem.TryGetMotionToPhoton().
    ///
    /// ⚠ PROXY SOFTWARE — no es motion-to-photon real medido con hardware externo.
    ///   TryGetMotionToPhoton() reporta la latencia estimada por el compositor Oculus
    ///   desde la última pose de tracking hasta el medio del scanout del frame actual.
    ///   Es un valor calculado por el runtime, NO una medición física directa.
    ///
    /// Razón de este enfoque: com.unity.xr.hands NO está instalado en este proyecto,
    /// por lo que XRHandSubsystem.updatedHands no está disponible. TryGetMotionToPhoton
    /// es la alternativa verificada en OculusPerformance.cs del paquete com.unity.xr.oculus 4.5.4.
    /// </summary>
    [RequireComponent(typeof(PerfSampler))]
    public sealed class HandLatencyProbe : MonoBehaviour
    {
        [Tooltip("Ventana del promedio móvil (frames)")]
        [Range(5, 60)]
        public int movingAverageWindow = 20;

        public float HandTrackingLatencyMs { get; private set; }

        private float[]                  _window;
        private int                      _windowHead;
        private float                    _windowSum;
        private List<XRDisplaySubsystem> _displays = new List<XRDisplaySubsystem>(1);
        private XRDisplaySubsystem       _display;
        private PerfSampler              _sampler;

        void Awake()
        {
            _sampler = GetComponent<PerfSampler>();
            _window  = new float[movingAverageWindow];
        }

        void OnEnable()
        {
            RefreshDisplay();
        }

        void LateUpdate()
        {
            RefreshDisplay();

            float rawMs = 0f;
            if (_display != null && _display.TryGetMotionToPhoton(out float motionToPhotonSec))
                rawMs = motionToPhotonSec * 1000f;

            // Actualiza promedio móvil (sin alloc)
            _windowSum -= _window[_windowHead];
            _window[_windowHead] = rawMs;
            _windowSum += rawMs;
            _windowHead = (_windowHead + 1) % movingAverageWindow;

            HandTrackingLatencyMs = _windowSum / movingAverageWindow;

            if (_sampler != null)
                _sampler.ExternalHandLatencyMs = HandTrackingLatencyMs;
        }

        void RefreshDisplay()
        {
            if (_display != null && _display.running) return;
            _displays.Clear();
            SubsystemManager.GetSubsystems(_displays);
            _display = _displays.Count > 0 ? _displays[0] : null;
        }
    }
}
