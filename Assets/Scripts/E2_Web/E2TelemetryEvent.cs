using System;
using UnityEngine;

namespace PhishingVR.E2
{
    [Serializable]
    public struct E2TelemetryEvent
    {
        public string SessionId;
        public string Scenario;       // siempre "E2"
        public string StimulusId;     // SiteData.SiteId
        public string Action;         // "report" | "credentials"
        public float ResponseTimeMs;
        public bool Correct;
        public long TimestampUnix;
    }
}
