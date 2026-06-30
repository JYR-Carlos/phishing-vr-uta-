using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

#if URP_AVAILABLE
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
#endif

namespace PhishVR.Telemetry
{
    /// <summary>
    /// Captura métricas de rendimiento por frame sin asignaciones de heap (GC-free en steady state).
    /// APIs usadas: FrameTimingManager (Unity 6), XRDisplaySubsystem.TryGetAppGPUTimeLastFrame,
    /// XRDisplaySubsystem.TryGetDroppedFrameCount — todas verificadas contra com.unity.xr.oculus 4.5.4.
    /// </summary>
    public sealed class PerfSampler : MonoBehaviour
    {
        public const int RingBufferSize = 512;

        // Pre-allocated ring buffer (structs, no GC)
        public struct FrameSample
        {
            public double TimestampSec;
            public float Fps;
            public float CpuFrameMs;
            public float GpuFrameMs;
            public int DroppedFramesDelta;
            public float HandLatencyMs;
            public string ConditionId;  // set externally by ExperimentRunner
            public int FrameIndex;
            public float FfrLevel;
            public float RenderScale;
            public int MsaaSampleCount;
        }

        public FrameSample[] Buffer { get; } = new FrameSample[RingBufferSize];
        public int WriteHead { get; private set; }
        public int TotalFrames { get; private set; }

        // Exposed latest values (read-only from other components)
        public float LatestFps { get; private set; }
        public float LatestCpuMs { get; private set; }
        public float LatestGpuMs { get; private set; }
        public int  LatestDroppedDelta { get; private set; }

        // Set externally by HandLatencyProbe
        public float ExternalHandLatencyMs { get; set; }

        // Set externally by ExperimentRunner / RenderConditionController
        public string CurrentConditionId { get; set; } = "none";
        public int    CurrentFfrLevel    { get; set; }
        public float  CurrentRenderScale { get; set; } = 1f;
        public int    CurrentMsaa        { get; set; } = 4;

        // ── Private state ──────────────────────────────────────────────────────
        private FrameTiming[]            _timings  = new FrameTiming[1];
        private List<XRDisplaySubsystem> _displays = new List<XRDisplaySubsystem>(1);
        private XRDisplaySubsystem       _display;
        private int                      _lastDroppedFrames;

        void OnEnable()
        {
            RefreshDisplaySubsystem();
        }

        void LateUpdate()
        {
            RefreshDisplaySubsystem();
            Sample();
        }

        void RefreshDisplaySubsystem()
        {
            if (_display != null && _display.running) return;
            _displays.Clear();
            SubsystemManager.GetSubsystems(_displays);
            _display = _displays.Count > 0 ? _displays[0] : null;
        }

        void Sample()
        {
            // ── CPU frame time (FrameTimingManager) ──────────────────────────
            FrameTimingManager.CaptureFrameTimings();
            uint got = FrameTimingManager.GetLatestTimings(1, _timings);
            float cpuMs = got > 0 ? (float)_timings[0].cpuFrameTime : Time.unscaledDeltaTime * 1000f;

            // ── GPU frame time (XRDisplaySubsystem — reliable on Quest 3) ───
            float gpuMs = 0f;
            if (_display != null)
            {
                if (_display.TryGetAppGPUTimeLastFrame(out float gpuSec))
                    gpuMs = gpuSec * 1000f;
            }
            // Fallback: FrameTimingManager gpu (often 0 on mobile Vulkan)
            if (gpuMs <= 0f && got > 0)
                gpuMs = (float)_timings[0].gpuFrameTime;

            // ── Dropped frames (cumulative → delta) ─────────────────────────
            int droppedDelta = 0;
            if (_display != null && _display.TryGetDroppedFrameCount(out int droppedTotal))
            {
                droppedDelta = droppedTotal - _lastDroppedFrames;
                if (droppedDelta < 0) droppedDelta = 0; // counter reset guard
                _lastDroppedFrames = droppedTotal;
            }

            float fps = 1f / Time.unscaledDeltaTime;

            // ── Write to ring buffer (no alloc) ─────────────────────────────
            ref FrameSample s = ref Buffer[WriteHead];
            s.TimestampSec    = Time.realtimeSinceStartupAsDouble;
            s.Fps             = fps;
            s.CpuFrameMs      = cpuMs;
            s.GpuFrameMs      = gpuMs;
            s.DroppedFramesDelta = droppedDelta;
            s.HandLatencyMs   = ExternalHandLatencyMs;
            s.ConditionId     = CurrentConditionId;
            s.FrameIndex      = TotalFrames;
            s.FfrLevel        = CurrentFfrLevel;
            s.RenderScale     = CurrentRenderScale;
            s.MsaaSampleCount = CurrentMsaa;

            WriteHead = (WriteHead + 1) % RingBufferSize;
            TotalFrames++;

            LatestFps         = fps;
            LatestCpuMs       = cpuMs;
            LatestGpuMs       = gpuMs;
            LatestDroppedDelta = droppedDelta;
        }
    }
}
