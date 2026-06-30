using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.XR.Oculus;

#if URP_AVAILABLE
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace PhishVR.Telemetry
{
    /// <summary>
    /// Aplica en runtime las condiciones de renderizado para el experimento A/B.
    ///
    /// APIs verificadas contra paquetes instalados:
    ///   FFR:          Unity.XR.Oculus.Utils.foveatedRenderingLevel  (FFR.cs — com.unity.xr.oculus 4.5.4)
    ///   Render scale: UnityEngine.XR.XRSettings.eyeTextureResolutionScale
    ///   MSAA:         QualitySettings.antiAliasing  (URP no instalado; #if URP_AVAILABLE para el futuro)
    ///   Refresh rate: Unity.XR.Oculus.Performance.TrySetDisplayRefreshRate  (OculusPerformance.cs)
    /// </summary>
    [RequireComponent(typeof(PerfSampler))]
    public sealed class RenderConditionController : MonoBehaviour
    {
        public RenderCondition ActiveCondition { get; private set; }

        private PerfSampler _sampler;

        void Awake()
        {
            _sampler = GetComponent<PerfSampler>();
        }

        public void Apply(RenderCondition condition)
        {
            ActiveCondition = condition;

            // ── Fixed Foveated Rendering ────────────────────────────────────
            // Utils.foveatedRenderingLevel: 0=off, 1=low, 2=medium, 3=high, 4=high-top
            Utils.foveatedRenderingLevel = condition.foveatedRenderingLevel;
            // Desactivar dynamic FFR para que el nivel fijo sea determinista
            Utils.useDynamicFoveatedRendering = false;

            // ── Eye texture resolution scale ────────────────────────────────
            XRSettings.eyeTextureResolutionScale = condition.renderScale;

            // ── MSAA ────────────────────────────────────────────────────────
#if URP_AVAILABLE
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urpAsset)
                urpAsset.msaaSampleCount = condition.msaaSampleCount;
#else
            // Sin URP: aplicar vía QualitySettings (afecta el antialiasing de la cámara)
            QualitySettings.antiAliasing = condition.msaaSampleCount;
#endif

            // ── Display refresh rate ────────────────────────────────────────
            if (condition.targetRefreshRate > 0f)
            {
                bool set = Performance.TrySetDisplayRefreshRate(condition.targetRefreshRate);
                if (!set)
                    Debug.LogWarning($"[RenderConditionController] No se pudo fijar refresh rate a {condition.targetRefreshRate} Hz");
            }

            // ── Actualiza PerfSampler ───────────────────────────────────────
            if (_sampler != null)
            {
                _sampler.CurrentConditionId = condition.conditionId;
                _sampler.CurrentFfrLevel    = condition.foveatedRenderingLevel;
                _sampler.CurrentRenderScale = condition.renderScale;
                _sampler.CurrentMsaa        = condition.msaaSampleCount;
            }

            Debug.Log($"[RenderConditionController] Condición aplicada: {condition.displayName} | " +
                      $"FFR={condition.foveatedRenderingLevel} scale={condition.renderScale:F2} MSAA={condition.msaaSampleCount}x");
        }

        public void RestoreDefaults()
        {
            Utils.foveatedRenderingLevel  = 0;
            XRSettings.eyeTextureResolutionScale = 1.0f;
            QualitySettings.antiAliasing  = 4;
        }
    }
}
