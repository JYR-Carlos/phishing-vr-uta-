using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace PhishVR.Telemetry.Editor
{
    /// <summary>
    /// Crea y configura la escena Assets/Scenes/PerfTest.unity con el prefab PerfHarness.
    /// Menu: PhishVR > Create PerfTest Scene
    /// </summary>
    public static class PerfTestSceneCreator
    {
        [MenuItem("PhishVR/Create PerfTest Scene")]
        public static void CreateScene()
        {
            const string scenePath = "Assets/Scenes/PerfTest.unity";

            // Pedir confirmación si ya existe
            if (System.IO.File.Exists(scenePath))
            {
                if (!EditorUtility.DisplayDialog("PerfTest Scene",
                    $"Ya existe {scenePath}. ¿Sobreescribir?", "Sí", "Cancelar"))
                    return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Buscar el prefab PerfHarness
            string[] guids = AssetDatabase.FindAssets("PerfHarness t:Prefab");
            if (guids.Length > 0)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    PrefabUtility.InstantiatePrefab(prefab);
                    Debug.Log($"[PerfTestSceneCreator] Prefab PerfHarness instanciado desde {prefabPath}");
                }
            }
            else
            {
                Debug.LogWarning("[PerfTestSceneCreator] Prefab 'PerfHarness' no encontrado. " +
                                 "Arrástralo manualmente a la escena.");
            }

            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[PerfTestSceneCreator] Escena guardada en {scenePath}");
        }
    }
}
