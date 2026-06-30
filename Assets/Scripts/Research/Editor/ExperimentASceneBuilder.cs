// Solo Editor — NO entra en la build. Construye la escena del Experimento A.
// Menú:  Tools → PhishingVR → Paper → Crear escena Experimento A
//
// Genera Assets/Scenes/Bench_ExperimentA.unity con:
//   - Directional Light
//   - Main Camera a altura de ojos
//   - AlertFieldOrigin (centro del campo de alertas, frente a la cámara)
//   - A_RenderBenchmark con el componente AlertRenderBenchmark ya cableado
//
// Para correr EN QUEST con render estéreo/passthrough: arrastra a la escena tu
// XR Origin (XR Rig) en lugar de la Main Camera plana antes de buildear.

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PhishingVR.Research.ExperimentA;

namespace PhishingVR.Research.Editor
{
    public static class ExperimentASceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Bench_ExperimentA.unity";

        [MenuItem("Tools/PhishingVR/Paper/Crear escena Experimento A")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Luz
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Cámara (plana; reemplazar por XR Origin para corridas en dispositivo)
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f);
            camGO.AddComponent<AudioListener>();
            camGO.transform.position = new Vector3(0f, 1.6f, 0f);

            // Origen del campo de alertas, 2.5 m al frente
            var originGO = new GameObject("AlertFieldOrigin");
            originGO.transform.position = new Vector3(0f, 1.6f, 2.5f);
            originGO.transform.rotation = Quaternion.identity;

            // Controlador del benchmark
            var benchGO = new GameObject("A_RenderBenchmark");
            var bench = benchGO.AddComponent<AlertRenderBenchmark>();

            // Cablear spawnOrigin -> AlertFieldOrigin vía SerializedObject
            var so = new SerializedObject(bench);
            var prop = so.FindProperty("spawnOrigin");
            if (prop != null)
            {
                prop.objectReferenceValue = originGO.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EnsureScenesFolder();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[ExperimentASceneBuilder] Escena creada → {ScenePath}\n" +
                      "Dale Play para generar los CSV en persistentDataPath/benchmarks/. " +
                      "Para Quest, sustituye la Main Camera por el XR Origin.");
        }

        private static void EnsureScenesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
