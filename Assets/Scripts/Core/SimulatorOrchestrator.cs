using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using PhishingVR.Core;

namespace PhishingVR.Core
{
    /// <summary>
    /// Controla el flujo: StartPanel → E1 → E2 → E3 → ResultsPanel.
    /// Vive en MainScene. Mathiu es el responsable de este script.
    /// </summary>
    public class SimulatorOrchestrator : MonoBehaviour
    {
        [Header("Escenas (nombres exactos en Build Settings)")]
        [SerializeField] private string sceneE1 = "E1_Email";
        [SerializeField] private string sceneE2 = "E2_Web";
        [SerializeField] private string sceneE3 = "E3_Smishing";

        [Header("UI")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private TMPro.TextMeshProUGUI resultsText;

        private int _scenariosCompleted = 0;

        void Start()
        {
            startPanel.SetActive(true);
            resultsPanel.SetActive(false);
        }

        // Llamar desde el botón "Iniciar Simulación" del StartPanel
        public void StartSimulation()
        {
            startPanel.SetActive(false);
            StartCoroutine(RunFlow());
        }

        private IEnumerator RunFlow()
        {
            yield return LoadScenario(sceneE1);
            yield return new WaitForSeconds(1f);
            yield return LoadScenario(sceneE2);
            yield return new WaitForSeconds(1f);
            yield return LoadScenario(sceneE3);
            yield return new WaitForSeconds(1f);
            ShowResults();
        }

        private IEnumerator LoadScenario(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            yield return op;

            // Buscar el IScenario en la escena recién cargada
            Scene loaded = SceneManager.GetSceneByName(sceneName);
            IScenario scenario = null;
            foreach (var go in loaded.GetRootGameObjects())
            {
                scenario = go.GetComponentInChildren<IScenario>(includeInactive: true) as IScenario;
                if (scenario != null) break;
            }

            if (scenario == null)
            {
                Debug.LogError($"[Orchestrator] No se encontró IScenario en {sceneName}");
                yield break;
            }

            bool done = false;
            scenario.OnScenarioCompleted += result =>
            {
                TelemetryManager.Instance.RegisterResult(result);
                done = true;
            };

            scenario.Activate();
            yield return new WaitUntil(() => done);
            scenario.Deactivate();

            yield return new WaitForSeconds(0.5f);
            SceneManager.UnloadSceneAsync(sceneName);
        }

        private void ShowResults()
        {
            resultsPanel.SetActive(true);
            var results = TelemetryManager.Instance.GetAllResults();
            int detected = 0;
            float totalTime = 0f;

            foreach (var r in results)
            {
                if (r.DetectedPhishing) detected++;
                totalTime += r.DecisionTimeMs;
            }

            float rate = results.Count > 0 ? (float)detected / results.Count * 100f : 0f;
            float avgTime = results.Count > 0 ? totalTime / results.Count : 0f;

            resultsText.text = $"Simulación completada\n\n" +
                               $"Detecciones correctas: {detected}/{results.Count} ({rate:F0}%)\n" +
                               $"Tiempo promedio de decisión: {avgTime:F0} ms";
        }
    }
}
