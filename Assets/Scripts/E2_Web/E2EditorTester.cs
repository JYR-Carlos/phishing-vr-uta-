// ────────────────────────────────────────────────────────────────────────────
// E2EditorTester.cs
// Tester de ratón SOLO para el Editor — NO entra en la build de Android/Quest.
//
// Permite mover la cámara y "pulsar" los botones de los World Space Canvas con el
// ratón, sin necesitar un Meta Quest y sin pelear con el XR Ray Interactor.
//
// Se instala SOLO: al darle Play en el Editor se añade automáticamente a la
// cámara principal. No hay que arrastrarlo a ningún GameObject.
//
// Controles:
//   - Mover:          WASD / flechas
//   - Subir / bajar:  E / Q
//   - Rotar cámara:   mantener clic DERECHO + mover ratón
//   - Pulsar botón:   clic IZQUIERDO (apunta con la mira del centro al botón)
//
// Por qué existe (y no se reutiliza Core/EditorTestController): la detección de
// clic de ese script usa EventSystem.RaycastAll, que el TrackedDeviceGraphicRaycaster
// ignora cuando el evento no viene de un dispositivo XR rastreado. Este tester
// hace su propio raycast geométrico contra el RectTransform del botón, así que
// funciona con cualquier raycaster (estándar o XR).
// ────────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PhishingVR.E2
{
    public class E2EditorTester : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField] private float maxClickDistance = 20f;

        private Camera _cam;
        private float _pitch;
        private float _yaw;

        // Se ejecuta una vez tras cargar la escena: se engancha solo a la cámara.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoAttach()
        {
            var cam = Camera.main;
            if (cam != null && cam.GetComponent<E2EditorTester>() == null)
                cam.gameObject.AddComponent<E2EditorTester>();
        }

        private void Start()
        {
            _cam = GetComponent<Camera>() != null ? GetComponent<Camera>() : Camera.main;

            Vector3 e = _cam.transform.eulerAngles;
            _yaw = e.y;
            _pitch = e.x;

            // Sin casco conectado, el Tracked Pose Driver fijaría la cámara y no
            // dejaría moverla con el ratón. Lo desactivamos solo en el Editor.
            foreach (var mb in _cam.GetComponents<MonoBehaviour>())
                if (mb != this && mb.GetType().Name.Contains("TrackedPoseDriver"))
                    mb.enabled = false;

            Debug.Log("[E2EditorTester] Activo. Clic DERECHO = rotar · WASD = mover · Clic IZQUIERDO = pulsar botón. (Solo Editor)");
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();
            if (Input.GetMouseButtonDown(0)) TryClickCenter();
        }

        private void HandleLook()
        {
            if (!Input.GetMouseButton(1)) return;
            _yaw += Input.GetAxis("Mouse X") * lookSensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
            _pitch = Mathf.Clamp(_pitch, -85f, 85f);
            _cam.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void HandleMovement()
        {
            Transform t = _cam.transform;
            Vector3 move = t.right * Input.GetAxis("Horizontal") + t.forward * Input.GetAxis("Vertical");
            if (Input.GetKey(KeyCode.E)) move += Vector3.up;
            if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;
            float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? 3f : 1f);
            t.position += move * speed * Time.deltaTime;
        }

        // Rayo desde el centro de la cámara; pulsa el Button más cercano cuyo
        // RectTransform sea atravesado por el rayo. Independiente del raycaster.
        private void TryClickCenter()
        {
            Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);
            Button best = null;
            float bestDist = maxClickDistance;

            foreach (var btn in FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (!btn.isActiveAndEnabled || !btn.interactable) continue;
                if (btn.transform is not RectTransform rt) continue;

                // Intersección rayo-plano del botón (válida apunte hacia donde apunte la cara).
                Vector3 n = rt.forward;
                float denom = Vector3.Dot(ray.direction, n);
                if (Mathf.Abs(denom) < 1e-6f) continue;
                float dist = Vector3.Dot(rt.position - ray.origin, n) / denom;
                if (dist <= 0f || dist > bestDist) continue;

                Vector3 worldHit = ray.GetPoint(dist);
                Vector2 local = rt.InverseTransformPoint(worldHit);
                if (!rt.rect.Contains(local)) continue;

                best = btn;
                bestDist = dist;
            }

            if (best == null) return;

            best.onClick.Invoke();
            Debug.Log($"[E2EditorTester] Clic en: {best.name}");
        }

        private void OnGUI()
        {
            float cx = Screen.width / 2f, cy = Screen.height / 2f, s = 8f, th = 2f;
            GUI.color = Color.green;
            GUI.DrawTexture(new Rect(cx - s, cy - th / 2f, s * 2f, th), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - th / 2f, cy - s, th, s * 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
#endif
