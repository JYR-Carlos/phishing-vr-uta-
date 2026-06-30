using UnityEngine;
using UnityEditor;
using System.IO;

namespace PhishVR.Telemetry.Editor
{
    /// <summary>
    /// Crea los cuatro ScriptableObjects de condición del paper en Assets/Data/Conditions/.
    /// Menu: PhishVR > Create Default Render Conditions
    /// </summary>
    public static class ConditionAssetCreator
    {
        private const string OutputDir = "Assets/Data/Conditions";

        [MenuItem("PhishVR/Create Default Render Conditions")]
        public static void CreateDefaultConditions()
        {
            if (!AssetDatabase.IsValidFolder(OutputDir))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Conditions");
            }

            Create("C0_Baseline", "Baseline (C0)",   0, 1.00f, 4, 0f);
            Create("C1_FFR",      "FFR High (C1)",    3, 1.00f, 4, 0f);
            Create("C2_FFR_Scale","FFR+Scale (C2)",   3, 0.85f, 2, 0f);
            Create("C3_Aggressive","Aggressive (C3)", 3, 0.70f, 1, 0f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ConditionAssetCreator] 4 condiciones creadas en {OutputDir}");
        }

        private static void Create(string assetName, string displayName,
                                   int ffr, float scale, int msaa, float hz)
        {
            string path = $"{OutputDir}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<RenderCondition>(path);
            if (existing != null)
            {
                Debug.Log($"[ConditionAssetCreator] Ya existe: {path} — omitido.");
                return;
            }

            var rc = ScriptableObject.CreateInstance<RenderCondition>();
            rc.conditionId           = assetName;
            rc.displayName           = displayName;
            rc.foveatedRenderingLevel = ffr;
            rc.renderScale           = scale;
            rc.msaaSampleCount       = msaa;
            rc.targetRefreshRate     = hz;

            AssetDatabase.CreateAsset(rc, path);
            Debug.Log($"[ConditionAssetCreator] Creado: {path}");
        }
    }
}
