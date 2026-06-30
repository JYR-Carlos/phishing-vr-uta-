using UnityEngine;
using PhishingVR.Core;

namespace PhishingVR.E2
{
    /// <summary>
    /// Cierra el ciclo de telemetría cuando E2 corre SOLO (escena oficina / E2_Web
    /// standalone), sin el SimulatorOrchestrator de Mathiu.
    ///
    /// - Se suscribe a <see cref="E2Manager.OnScenarioCompleted"/>.
    /// - Asegura que exista un <see cref="TelemetryManager"/> en escena (lo crea si falta).
    /// - Al terminar el escenario, registra el <see cref="ScenarioResult"/> → JSON de sesión
    ///   en Application.persistentDataPath/session_*.json.
    ///
    /// La telemetría por-decisión (una fila por sitio) ya la escribe E2ApiClient en CSV.
    /// Este componente añade el RESUMEN de la sesión (detección, tiempo medio, perfil, riskScore).
    ///
    /// Uso: agrégalo a un GameObject de la escena (p. ej. el mismo E2Manager o uno vacío
    /// "E2_Telemetry"). Si dejas el campo vacío, busca el E2Manager automáticamente.
    /// </summary>
    public sealed class E2StandaloneTelemetry : MonoBehaviour
    {
        [Tooltip("Si se deja vacío, se busca el E2Manager de la escena en Start().")]
        [SerializeField] private E2Manager manager;

        private bool _subscribed;

        private void Start()
        {
            if (manager == null) manager = FindFirstObjectByType<E2Manager>();
            if (manager == null)
            {
                Debug.LogWarning("[E2StandaloneTelemetry] No encontré un E2Manager en la escena.");
                return;
            }

            EnsureTelemetryManager();
            manager.OnScenarioCompleted += OnCompleted;
            _subscribed = true;
        }

        private void OnDestroy()
        {
            if (_subscribed && manager != null)
                manager.OnScenarioCompleted -= OnCompleted;
        }

        private void OnCompleted(ScenarioResult result)
        {
            TelemetryManager.Instance?.RegisterResult(result);
            Debug.Log($"[E2StandaloneTelemetry] Sesión registrada → " +
                      $"detectó={result.DetectedPhishing}, t_medio={result.DecisionTimeMs:F0}ms, " +
                      $"perfil={result.BehavioralProfile}, risk={result.RiskScore:F2}");
        }

        private static void EnsureTelemetryManager()
        {
            if (TelemetryManager.Instance != null) return;
            var go = new GameObject("TelemetryManager");
            go.AddComponent<TelemetryManager>();
            Debug.Log("[E2StandaloneTelemetry] TelemetryManager creado para la sesión standalone.");
        }
    }
}
