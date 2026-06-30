using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using PhishingVR.Research;

namespace PhishingVR.E2
{
    public static class E2ApiClient
    {
        public static string BaseUrl = "http://localhost:5000/api/events";

        // Telemetría persistente por-decisión: SIEMPRE se escribe a CSV en
        // Application.persistentDataPath/benchmarks/e2_telemetry_*.csv, además del
        // stub de consola o el POST a Flask. Es la fuente de datos del análisis del
        // escenario web (una fila por cada decisión del usuario).
        private static BenchmarkCsvLogger _csv;
        private static BenchmarkCsvLogger Csv => _csv ??= new BenchmarkCsvLogger(
            "e2_telemetry",
            "session_id,scenario,stimulus_id,action,response_time_ms,correct,timestamp_unix");

        public static IEnumerator PostEvent(E2TelemetryEvent evt)
        {
            string json = JsonUtility.ToJson(evt);

            // CSV (flush por fila: en Quest la app puede cerrarse sin OnApplicationQuit).
            try
            {
                Csv.WriteRow(evt.SessionId, evt.Scenario, evt.StimulusId, evt.Action,
                             evt.ResponseTimeMs, evt.Correct, evt.TimestampUnix);
                Csv.Flush();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[E2ApiClient] No se pudo escribir el CSV de telemetría: {e.Message}");
            }

#if PHISHINGVR_FLASK_ENABLED
            byte[] body = Encoding.UTF8.GetBytes(json);
            using var req = new UnityWebRequest(BaseUrl, "POST");
            req.uploadHandler   = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"[E2ApiClient] {req.error}");
#else
            Debug.Log($"[E2ApiClient] stub → {json}");
            yield break;
#endif
        }
    }
}
