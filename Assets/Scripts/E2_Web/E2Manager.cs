using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PhishingVR.Core;

namespace PhishingVR.E2
{
    public class E2Manager : MonoBehaviour, IScenario
    {
        public event Action<ScenarioResult> OnScenarioCompleted;

        [Header("Sitios — arrastrar SiteData assets en orden")]
        [SerializeField] private List<SiteData> sites;

        [Header("Componentes")]
        [SerializeField] private BrowserController browser;
        [SerializeField] private E2FeedbackPanel feedbackPanel;

        [Header("Botones (UI Button en World Space Canvas)")]
        [SerializeField] private Button btnReportSite;
        [SerializeField] private Button btnEnterCredentials;

        private enum UserAction { None, Report, EnterCredentials }

        private UserAction _pendingAction;
        private float _stimulusStartTime;

        private int _phishingTotal;
        private int _phishingDetected;
        private float _totalDecisionTimeMs;
        private int _siteCount;
        private string _sessionId;

        void Awake()
        {
            _sessionId = Guid.NewGuid().ToString("N")[..8];
        }
        void Start() => Activate(); // <- Agrega solo
        public void Activate()
        {
            gameObject.SetActive(true);
            if (btnReportSite == null || btnEnterCredentials == null)
            {
                Debug.LogError("[E2Manager] Faltan referencias de botones en el Inspector. " +
                               "Arrastra 'Btn_CerrarPestana' → btnReportSite y " +
                               "'Btn_IngresarDatos' → btnEnterCredentials.");
                return;
            }
            btnReportSite.onClick.AddListener(OnReport);
            btnEnterCredentials.onClick.AddListener(OnEnterCredentials);
            StartCoroutine(RunSequence());
        }

        public void Deactivate()
        {
            StopAllCoroutines();
            btnReportSite.onClick.RemoveListener(OnReport);
            btnEnterCredentials.onClick.RemoveListener(OnEnterCredentials);
            gameObject.SetActive(false);
        }

        private void OnReport() => _pendingAction = UserAction.Report;
        private void OnEnterCredentials() => _pendingAction = UserAction.EnterCredentials;

        private IEnumerator RunSequence()
        {
            _phishingTotal = 0;
            _phishingDetected = 0;
            _totalDecisionTimeMs = 0f;
            _siteCount = 0;

            foreach (var site in sites)
            {
                if (site.IsPhishing) _phishingTotal++;

                browser.ShowSite(site);
                _pendingAction = UserAction.None;
                _stimulusStartTime = Time.time;

                yield return new WaitUntil(() => _pendingAction != UserAction.None);

                float responseMs = (Time.time - _stimulusStartTime) * 1000f;
                _totalDecisionTimeMs += responseMs;
                _siteCount++;

                bool reported = _pendingAction == UserAction.Report;
                bool correct = site.IsPhishing ? reported : !reported;
                bool falsePositive = !site.IsPhishing && reported;

                if (site.IsPhishing && reported)
                    _phishingDetected++;

                var evt = new E2TelemetryEvent
                {
                    SessionId = _sessionId,
                    Scenario = "E2",
                    StimulusId = site.SiteId,
                    Action = reported ? "report" : "credentials",
                    ResponseTimeMs = responseMs,
                    Correct = correct,
                    TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                StartCoroutine(E2ApiClient.PostEvent(evt));

                yield return feedbackPanel.Show(correct, falsePositive, site);
                yield return new WaitForSeconds(0.5f);
            }

            FinishScenario();
        }

        private void FinishScenario()
        {
            float avgTimeMs = _siteCount > 0 ? _totalDecisionTimeMs / _siteCount : 0f;
            float riskScore = _phishingTotal > 0
                ? 1f - (float)_phishingDetected / _phishingTotal
                : 0f;
            bool allDetected = _phishingTotal > 0 && _phishingDetected == _phishingTotal;

            string profile = avgTimeMs < 5000f ? "Impulsive"
                           : avgTimeMs < 15000f ? "Intermediate"
                           : "Cautious";

            OnScenarioCompleted?.Invoke(new ScenarioResult
            {
                ScenarioId = "scenario_2",
                DetectedPhishing = allDetected,
                DecisionTimeMs = avgTimeMs,
                BehavioralProfile = profile,
                RiskScore = riskScore,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
