// Solo Editor — NO entra en la build. Integra el escenario E2 con el set de oficina
// "VNB - Office Set" sobre la escena ABIERTA.
//
// Menús (Tools → PhishingVR → E2 Oficina → ...):
//   1) Asegurar XR + EventSystem        → añade XR Origin (ray), XR Interaction Manager
//                                          y EventSystem si faltan (para la escena demo del set).
//   2) Montar navegador en el monitor   → ancla E2_BrowserCanvas a la cara del objeto
//                                          SELECCIONADO (el monitor), mirando al usuario.
//   3) Feedback frente al usuario        → coloca E2_FeedbackCanvas a la altura de los ojos.
//   4) Alternar geometría placeholder    → oculta/restaura E2_Wall_Back / E2_Floor / E2_Desk.
//
// Flujo recomendado (vistiendo TU escena E2 funcional, sin romper cableado):
//   a. Abre Assets/Scenes/E2_Web.unity.
//   b. Arrastra a la escena los prefabs del set: Office/desk_3_alt, Office/new_monitor,
//      Office/new_case, Office/office_chair, y piso/paredes de Building/ (ubícalos frente al rig).
//   c. Selecciona el new_monitor en la jerarquía → menú "Montar navegador en el monitor".
//   d. Menú "Alternar geometría placeholder" para esconder el escritorio/paredes grises.
//   e. (Opcional) menú "Feedback frente al usuario".
//
// Si prefieres partir de la escena demo del set: ábrela, corre "Asegurar XR + EventSystem",
// y trae los objetos E2 manualmente (multi-selección → arrastrar entre escenas conserva refs).

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace PhishingVR.E2.Editor
{
    public static class E2OfficeIntegration
    {
        // GUID del prefab "XR Origin (XR Rig)" del sample XRI 3.0.11 (ray interactors incluidos).
        private const string XrOriginGuid = "f6336ac4ac8b4d34bc5072418cdc62a0";

        private const string BrowserCanvasName  = "E2_BrowserCanvas";
        private const string FeedbackCanvasName = "E2_FeedbackCanvas";

        private static readonly string[] PlaceholderNames =
            { "E2_Wall_Back", "E2_Floor", "E2_Desk" };

        // ── 1) XR + EventSystem ──────────────────────────────────────────────
        [MenuItem("Tools/PhishingVR/E2 Oficina/1) Asegurar XR + EventSystem")]
        public static void EnsureXrAndEventSystem()
        {
            // XR Origin
            if (Object.FindFirstObjectByType<XROrigin>() == null)
            {
                string path = AssetDatabase.GUIDToAssetPath(XrOriginGuid);
                var prefab = string.IsNullOrEmpty(path)
                    ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    inst.transform.position = Vector3.zero;
                    Debug.Log("[E2Office] XR Origin instanciado.");
                }
                else
                {
                    Debug.LogWarning("[E2Office] No se encontró el prefab XR Origin del sample XRI. " +
                                     "Importa 'Starter Assets' de XR Interaction Toolkit.");
                }
            }

            // XR Interaction Manager
            if (Object.FindFirstObjectByType<XRInteractionManager>() == null)
            {
                var go = new GameObject("XR Interaction Manager");
                go.AddComponent<XRInteractionManager>();
                Debug.Log("[E2Office] XR Interaction Manager creado.");
            }

            // EventSystem con módulo de entrada XR
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<XRUIInputModule>();
                Debug.Log("[E2Office] EventSystem + XRUIInputModule creados.");
            }

            WarnExtraCameras();
            MarkDirty();
        }

        // ── 2) Anclar el navegador al monitor seleccionado ───────────────────
        [MenuItem("Tools/PhishingVR/E2 Oficina/2) Montar navegador en el monitor")]
        public static void MountBrowserOnSelectedMonitor()
        {
            var monitor = Selection.activeTransform;
            if (monitor == null)
            {
                Debug.LogError("[E2Office] Selecciona primero el monitor (new_monitor) en la jerarquía.");
                return;
            }

            var canvas = GameObject.Find(BrowserCanvasName);
            if (canvas == null)
            {
                Debug.LogError($"[E2Office] No encuentro '{BrowserCanvasName}' activo en la escena. " +
                               "Asegúrate de tener la escena E2 cargada y el canvas activo.");
                return;
            }

            Renderer rend = monitor.GetComponentInChildren<Renderer>();
            if (rend == null)
            {
                Debug.LogError("[E2Office] El objeto seleccionado no tiene Renderer; selecciona la malla del monitor.");
                return;
            }

            Bounds b = rend.bounds;                 // en mundo
            Camera cam = GetXrCamera();

            // Dirección monitor → usuario (horizontal); fallback al forward del monitor.
            Vector3 toUser = cam != null ? (cam.transform.position - b.center) : monitor.forward;
            toUser.y = 0f;
            if (toUser.sqrMagnitude < 1e-4f) toUser = monitor.forward;
            toUser.Normalize();

            Undo.RecordObject(canvas.transform, "Montar navegador en monitor");

            // Posición: justo delante de la cara del monitor, hacia el usuario.
            float depthOffset = b.extents.magnitude * 0.5f + 0.02f;
            Vector3 pos = b.center + toUser * depthOffset;

            // El contenido de un World Space Canvas se lee cuando su +Z apunta en la
            // misma dirección que mira la cámara, es decir, hacia el monitor (−toUser).
            Quaternion rot = Quaternion.LookRotation(-toUser, Vector3.up);

            canvas.transform.SetPositionAndRotation(pos, rot);
            FitCanvasHeight(canvas.transform as RectTransform, canvas.transform, b.size.y * 0.92f);

            // Parentar al monitor para que se mueva con él (conserva transform de mundo).
            // Si el canvas es instancia de prefab, hay que desempacarlo primero.
            if (PrefabUtility.IsPartOfPrefabInstance(canvas))
            {
                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(canvas);
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                Debug.Log($"[E2Office] '{canvas.name}' desempacado del prefab para poder parentarlo.");
            }
            canvas.transform.SetParent(monitor, worldPositionStays: true);

            EnsureTrackedRaycaster(canvas);
            Selection.activeGameObject = canvas;
            Debug.Log($"[E2Office] '{BrowserCanvasName}' anclado al monitor. " +
                      "Si el texto sale espejado, gira el canvas 180° en Y.");
            MarkDirty();
        }

        // ── 3) Feedback frente al usuario ────────────────────────────────────
        [MenuItem("Tools/PhishingVR/E2 Oficina/3) Feedback frente al usuario")]
        public static void PlaceFeedbackInFront()
        {
            var canvas = GameObject.Find(FeedbackCanvasName);
            if (canvas == null)
            {
                Debug.LogError($"[E2Office] No encuentro '{FeedbackCanvasName}' activo en la escena.");
                return;
            }
            Camera cam = GetXrCamera();
            if (cam == null)
            {
                Debug.LogError("[E2Office] No hay cámara XR. Corre primero 'Asegurar XR + EventSystem'.");
                return;
            }

            Undo.RecordObject(canvas.transform, "Colocar feedback");
            Vector3 fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
            canvas.transform.position = cam.transform.position + fwd * 1.4f + Vector3.up * 0.0f;
            canvas.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            EnsureTrackedRaycaster(canvas);
            Debug.Log($"[E2Office] '{FeedbackCanvasName}' colocado a 1.4 m frente a la cámara.");
            MarkDirty();
        }

        // ── 4) Alternar geometría placeholder ────────────────────────────────
        [MenuItem("Tools/PhishingVR/E2 Oficina/4) Alternar geometria placeholder")]
        public static void TogglePlaceholderGeometry()
        {
            int toggled = 0;
            foreach (string name in PlaceholderNames)
            {
                var go = GameObject.Find(name);
                if (go == null) continue;
                Undo.RecordObject(go, "Alternar placeholder");
                go.SetActive(!go.activeSelf);
                toggled++;
            }
            Debug.Log(toggled == 0
                ? "[E2Office] No encontré geometría placeholder activa (E2_Wall_Back/E2_Floor/E2_Desk)."
                : $"[E2Office] Geometría placeholder alternada ({toggled} objetos).");
            MarkDirty();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static Camera GetXrCamera()
        {
            var origin = Object.FindFirstObjectByType<XROrigin>();
            if (origin != null && origin.Camera != null) return origin.Camera;
            return Camera.main;
        }

        // Escala uniforme el canvas para que su altura de mundo ≈ targetWorldHeight.
        private static void FitCanvasHeight(RectTransform rt, Transform t, float targetWorldHeight)
        {
            if (rt == null) return;
            float curWorldH = rt.rect.height * t.lossyScale.y;
            if (curWorldH < 1e-4f) return;
            float k = targetWorldHeight / curWorldH;
            t.localScale *= k;
        }

        // El Canvas necesita TrackedDeviceGraphicRaycaster (XRI) para que el ray lo golpee.
        private static void EnsureTrackedRaycaster(GameObject canvasGo)
        {
            var canvas = canvasGo.GetComponent<Canvas>();
            if (canvas == null) return;

            // Quitar GraphicRaycaster estándar si lo tuviera.
            var std = canvasGo.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            var tracked = canvasGo.GetComponent<TrackedDeviceGraphicRaycaster>();
            if (tracked == null)
            {
                if (std != null) Object.DestroyImmediate(std);
                canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();
                Debug.Log($"[E2Office] TrackedDeviceGraphicRaycaster añadido a '{canvasGo.name}'.");
            }
        }

        private static void WarnExtraCameras()
        {
            var xrCam = GetXrCamera();
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (cam == xrCam) continue;
                Debug.LogWarning($"[E2Office] Hay otra cámara en la escena ('{cam.name}'). " +
                                 "Desactívala o quita su AudioListener para evitar conflictos con el XR Origin.");
            }
        }

        private static void MarkDirty()
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
#endif
