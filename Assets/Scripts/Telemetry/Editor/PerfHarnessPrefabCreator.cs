using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace PhishVR.Telemetry.Editor
{
    /// <summary>
    /// Crea el prefab PerfHarness en Assets/Prefabs/PerfHarness.prefab.
    /// Agrupación de todos los componentes de instrumentación lista para arrastrar a una escena.
    /// Menu: PhishVR > Create PerfHarness Prefab
    /// </summary>
    public static class PerfHarnessPrefabCreator
    {
        [MenuItem("PhishVR/Create PerfHarness Prefab")]
        public static void CreatePrefab()
        {
            const string prefabPath = "Assets/Prefabs/PerfHarness.prefab";

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            // ── Root ──────────────────────────────────────────────────────────
            var root = new GameObject("PerfHarness");

            var sampler    = root.AddComponent<PerfSampler>();
            var probe      = root.AddComponent<HandLatencyProbe>();
            var controller = root.AddComponent<RenderConditionController>();
            var runner     = root.AddComponent<ExperimentRunner>();
            var logger     = root.AddComponent<CsvLogger>();

            // ── HUD Canvas (World Space, pequeño, anclado a cámara) ───────────
            var hudGo     = new GameObject("PerfHud");
            hudGo.transform.SetParent(root.transform);
            hudGo.transform.localPosition = new Vector3(0f, 0f, 0.5f);
            hudGo.transform.localScale    = Vector3.one * 0.001f; // 1mm per unit

            var canvas        = hudGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = hudGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 200);

            hudGo.AddComponent<CanvasScaler>();
            hudGo.AddComponent<GraphicRaycaster>();

            // Texto del HUD
            var textGo  = new GameObject("HudText");
            textGo.transform.SetParent(hudGo.transform, false);
            var textRt  = textGo.AddComponent<RectTransform>();
            textRt.anchorMin  = Vector2.zero;
            textRt.anchorMax  = Vector2.one;
            textRt.sizeDelta  = Vector2.zero;
            textRt.offsetMin  = new Vector2(10, 10);
            textRt.offsetMax  = new Vector2(-10, -10);

            var text = textGo.AddComponent<Text>();
            text.text      = "PerfHud — esperando datos...";
            text.fontSize  = 24;
            text.color     = Color.green;
            text.alignment = TextAnchor.UpperLeft;
            text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // ── PerfHud MonoBehaviour ─────────────────────────────────────────
            var hud    = hudGo.AddComponent<PerfHud>();
            hud.sampler   = sampler;
            hud.handProbe = probe;
            hud.runner    = runner;
            hud.hudText   = text;

            // ── Guardar prefab ────────────────────────────────────────────────
            bool success;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out success);
            Object.DestroyImmediate(root);

            if (success)
                Debug.Log($"[PerfHarnessPrefabCreator] Prefab guardado en {prefabPath}\n" +
                          "Configura en el Inspector:\n" +
                          "  • ExperimentRunner.conditions → arrastra los 4 assets de Conditions/\n" +
                          "  • PerfHud → asigna toggleAction si quieres ocultar con botón");
            else
                Debug.LogError("[PerfHarnessPrefabCreator] Error guardando el prefab.");

            AssetDatabase.Refresh();
        }
    }
}
