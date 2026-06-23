using System;

namespace PhishingVR.Core
{
    /// <summary>
    /// Contrato que los 3 escenarios deben implementar.
    /// NO modificar sin avisar al equipo.
    /// </summary>
    public interface IScenario
    {
        void Activate();
        void Deactivate();
        event Action<ScenarioResult> OnScenarioCompleted;
    }
}
