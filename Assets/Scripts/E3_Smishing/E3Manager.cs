using System;
using System.Collections;
using UnityEngine;
using PhishingVR.Core;

namespace PhishingVR.E3
{
    /// <summary>
    /// Controlador principal del Escenario 3: Smishing (SMS Phishing).
    /// Implementa IScenario para que el SimulatorOrchestrator lo pueda llamar.
    ///
    /// Flujo:
    ///   Activate() → el usuario ve/agarra el teléfono → SMS aparece →
    ///   usuario presiona Btn_ClickSMSLink o Btn_IgnoreSMS →
    ///   FinishScenario() → OnScenarioCompleted?.Invoke(result)
    /// </summary>
    public class E3Manager : MonoBehaviour, IScenario
    {
        // ── Evento requerido por IScenario ──────────────────────────────────
        public event Action<ScenarioResult> OnScenarioCompleted;

        // ── Referencias a los otros scripts (arrastra en el Inspector) ──────
        [Header("Sub-controladores")]
        [SerializeField] private SmartphoneController smartphoneController;
        [SerializeField] private E3UIController uiController;

        // ── Panel de feedback (aparece al terminar) ──────────────────────────
        [Header("Feedback")]
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private TMPro.TextMeshProUGUI feedbackText;

        // ── Tiempo que el feedback se muestra antes de cerrar el escenario ──
        [SerializeField] private float feedbackDurationSeconds = 3f;

        // ── Estado interno ───────────────────────────────────────────────────
        private float _startTime;
        private bool _scenarioActive;

        // ════════════════════════════════════════════════════════════════════
        //  IScenario — llamado por SimulatorOrchestrator
        // ════════════════════════════════════════════════════════════════════

        public void Activate()
        {
            gameObject.SetActive(true);

            // Ocultar feedback si quedó de una ejecución anterior
            if (feedbackPanel != null) feedbackPanel.SetActive(false);

            // Avisar a los sub-controladores
            smartphoneController?.OnScenarioStart();
            uiController?.HideButtons();

            // Iniciar timer
            _startTime = Time.time;
            _scenarioActive = true;

            Debug.Log("[E3Manager] Escenario Smishing activado.");
        }

        public void Deactivate()
        {
            _scenarioActive = false;
            gameObject.SetActive(false);
            Debug.Log("[E3Manager] Escenario Smishing desactivado.");
        }

        // ════════════════════════════════════════════════════════════════════
        //  Callback llamado por E3UIController cuando el usuario decide
        // ════════════════════════════════════════════════════════════════════

        /// <param name="clickedLink">true = cayó en la trampa / false = ignoró el SMS</param>
        public void OnUserDecision(bool clickedLink)
        {
            if (!_scenarioActive) return;
            _scenarioActive = false;

            float decisionTimeMs = (Time.time - _startTime) * 1000f;

            // Calcular perfil conductual en base al tiempo
            string profile = CalculateBehavioralProfile(decisionTimeMs, clickedLink);

            // RiskScore: penaliza caer en la trampa y decidir rápido
            float riskScore = CalculateRiskScore(clickedLink, decisionTimeMs);

            var result = new ScenarioResult
            {
                ScenarioId        = "scenario_3",
                DetectedPhishing  = !clickedLink,   // detectó = NO hizo clic
                DecisionTimeMs    = decisionTimeMs,
                BehavioralProfile = profile,
                RiskScore         = riskScore,
                Timestamp         = DateTime.UtcNow
            };

            // Registrar en telemetría global (Singleton de Mathiu)
            TelemetryManager.Instance?.RegisterResult(result);

            // Mostrar feedback breve antes de cerrar
            ShowFeedback(clickedLink, decisionTimeMs);

            // Esperar y luego disparar el evento para que el Orchestrator avance
            StartCoroutine(FinishAfterDelay(result, feedbackDurationSeconds));
        }

        // ════════════════════════════════════════════════════════════════════
        //  Helpers privados
        // ════════════════════════════════════════════════════════════════════

        private IEnumerator FinishAfterDelay(ScenarioResult result, float delay)
        {
            yield return new WaitForSeconds(delay);
            OnScenarioCompleted?.Invoke(result);
        }

        private string CalculateBehavioralProfile(float timeMs, bool clickedLink)
        {
            // Si ignoró el SMS independientemente del tiempo → Cautious
            if (!clickedLink && timeMs > 5000f) return "Cautious";

            // Si cayó rápido → Impulsive
            if (clickedLink && timeMs < 3000f) return "Impulsive";

            return "Intermediate";
        }

        private float CalculateRiskScore(bool clickedLink, float timeMs)
        {
            float score = 0f;

            // Caer en la trampa es el mayor factor
            if (clickedLink) score += 0.6f;

            // Decisión muy rápida suma riesgo (sin importar resultado)
            if (timeMs < 3000f)       score += 0.3f;
            else if (timeMs < 6000f)  score += 0.1f;

            return Mathf.Clamp01(score);
        }

        private void ShowFeedback(bool clickedLink, float timeMs)
        {
            if (feedbackPanel == null) return;

            feedbackPanel.SetActive(true);

            if (feedbackText == null) return;

            if (clickedLink)
            {
                feedbackText.text =
                    "⚠️ Caíste en un SMS de phishing.\n" +
                    "Los mensajes urgentes con links son una señal de alerta.\n" +
                    $"Tiempo de decisión: {timeMs / 1000f:F1}s";
                feedbackText.color = new Color(1f, 0.3f, 0.3f); // rojo
            }
            else
            {
                feedbackText.text =
                    "✅ ¡Bien hecho! Ignoraste el SMS sospechoso.\n" +
                    "Nunca hagas clic en links de mensajes desconocidos.\n" +
                    $"Tiempo de decisión: {timeMs / 1000f:F1}s";
                feedbackText.color = new Color(0.3f, 1f, 0.4f); // verde
            }
        }
    }
}
