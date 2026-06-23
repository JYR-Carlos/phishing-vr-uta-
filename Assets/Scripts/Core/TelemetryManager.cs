using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PhishingVR.Core;

namespace PhishingVR.Core
{
    /// <summary>
    /// Singleton. Recibe resultados de los 3 escenarios y los guarda en JSON.
    /// Mathiu es el responsable de este script.
    /// </summary>
    public class TelemetryManager : MonoBehaviour
    {
        public static TelemetryManager Instance { get; private set; }

        private readonly List<ScenarioResult> _results = new();
        private string _sessionId;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _sessionId = Guid.NewGuid().ToString("N")[..8];
        }

        public void RegisterResult(ScenarioResult result)
        {
            _results.Add(result);
            Debug.Log($"[Telemetry] {result.ScenarioId} — Detectó: {result.DetectedPhishing} — Tiempo: {result.DecisionTimeMs:F0}ms");
            SaveToFile();
        }

        public List<ScenarioResult> GetAllResults() => _results;

        private void SaveToFile()
        {
            var wrapper = new SessionData { SessionId = _sessionId, Results = _results };
            string json = JsonUtility.ToJson(wrapper, prettyPrint: true);
            string path = Path.Combine(Application.persistentDataPath, $"session_{_sessionId}.json");
            File.WriteAllText(path, json);
        }

        [Serializable]
        private class SessionData
        {
            public string SessionId;
            public List<ScenarioResult> Results;
        }
    }
}
