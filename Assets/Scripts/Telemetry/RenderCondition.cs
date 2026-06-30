using UnityEngine;

namespace PhishVR.Telemetry
{
    [CreateAssetMenu(fileName = "NewCondition", menuName = "PhishVR/Render Condition")]
    public class RenderCondition : ScriptableObject
    {
        [Tooltip("Identificador corto para el CSV (ej: C0_Baseline)")]
        public string conditionId = "C0";

        [Tooltip("Nombre legible para el HUD")]
        public string displayName = "Baseline";

        [Range(0, 4), Tooltip("0=off 1=low 2=medium 3=high 4=high-top")]
        public int foveatedRenderingLevel = 0;

        [Range(0.5f, 2.0f), Tooltip("Escala de la eye texture (1.0 = nativo)")]
        public float renderScale = 1.0f;

        [Tooltip("MSAA: 1, 2, 4 u 8. Con URP: valor aplicado a UniversalRenderPipelineAsset.msaaSampleCount")]
        public int msaaSampleCount = 4;

        [Tooltip("Hz objetivo (0 = no cambiar). Quest 3 soporta 72/90/120 Hz)")]
        public float targetRefreshRate = 0f;
    }
}
