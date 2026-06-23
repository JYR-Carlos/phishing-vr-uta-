using System;

namespace PhishingVR.Core
{
    /// <summary>
    /// Datos que cada escenario entrega al Orchestrator al terminar.
    /// </summary>
    [Serializable]
    public class ScenarioResult
    {
        public string ScenarioId;           // "scenario_1", "scenario_2", "scenario_3"
        public bool DetectedPhishing;       // true = identificó el phishing correctamente
        public float DecisionTimeMs;        // tiempo desde que apareció el estímulo hasta la decisión
        public string BehavioralProfile;    // "Cautious", "Intermediate", "Impulsive" (lo calcula cada manager)
        public float RiskScore;             // 0.0 a 1.0
        public DateTime Timestamp;          // cuándo terminó el escenario
    }
}
