// ────────────────────────────────────────────────────────────────────────────
// EditorTestController.cs
// Controlador de prueba para el Editor de Unity (NO se incluye en la build).
//
// Cómo usarlo:
//   1. Agrega este script a la Main Camera del XR Origin
//   2. Dale Play
//   3. Controles:
//      - Botón derecho del mouse → rotar cámara
//      - WASD / flechas           → mover
//      - E / Q                    → subir / bajar
//      - Clic izquierdo           → interactuar con botones UI y objetos
//      - Rueda del mouse          → zoom (velocidad)
//
// IMPORTANTE: Solo funciona en el Editor. En build Android/Quest este script
// no hace nada (el #if UNITY_EDITOR lo excluye del compilado final).
// ────────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace PhishingVR.Core
{
    /// <summary>
    /// Controlador FPS simple para probar las escenas en el Editor sin Meta Quest.
    /// Agrega este script a la Main Camera del XR Origin.
    /// </summary>
    public class EditorTestController : MonoBehaviour
    {
        [Header("Movimiento")]
        [SerializeField] private float moveSpeed    = 3f;
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField] private float verticalSpeed = 2f;

        [Header("Interacción")]
        [SerializeField] private float interactRange = 10f;
        [Tooltip("Color del crosshair cuando apunta a algo interactuable")]
        [SerializeField] private Color crosshairActive   = Color.green;
        [SerializeField] private Color crosshairInactive = Color.white;

        // ── Estado interno ───────────────────────────────────────────────────
        private float _pitch; // rotación vertical
        private float _yaw;   // rotación horizontal
        private bool  _lookMode;

        // Crosshair (se dibuja con OnGUI)
        private bool _canInteract;

        // ════════════════════════════════════════════════════════════════════
        //  Unity lifecycle
        // ════════════════════════════════════════════════════════════════════

        private void Start()
        {
            // Inicializar rotación desde la rotación actual de la cámara
            _yaw   = transform.eulerAngles.y;
            _pitch = transform.eulerAngles.x;

            Debug.Log("[EditorTestController] Activo. Botón DERECHO del mouse = rotar. WASD = mover. Clic IZQUIERDO = interactuar.");
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();
            HandleInteraction();
        }

        private void OnGUI()
        {
            DrawCrosshair();
        }

        // ════════════════════════════════════════════════════════════════════
        //  Movimiento y rotación
        // ════════════════════════════════════════════════════════════════════

        private void HandleLook()
        {
            // Activar modo rotación con botón derecho
            _lookMode = Input.GetMouseButton(1);

            if (!_lookMode) return;

            _yaw   += Input.GetAxis("Mouse X") * lookSensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
            _pitch  = Mathf.Clamp(_pitch, -80f, 80f);

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void HandleMovement()
        {
            float h = Input.GetAxis("Horizontal"); // A/D o flechas
            float v = Input.GetAxis("Vertical");   // W/S o flechas

            Vector3 move = transform.right * h + transform.forward * v;

            // Subir/bajar con E/Q
            if (Input.GetKey(KeyCode.E)) move += Vector3.up;
            if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;

            // Velocidad extra con Shift
            float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? 3f : 1f);

            transform.position += move * speed * Time.deltaTime;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Interacción con UI y objetos
        // ════════════════════════════════════════════════════════════════════

        private void HandleInteraction()
        {
            Ray ray = new Ray(transform.position, transform.forward);
            _canInteract = false;

            // 1. Probar UI (World Space Canvas)
            if (TryHitUI(ray))
            {
                _canInteract = true;
                if (Input.GetMouseButtonDown(0))
                    ClickUI(ray);
                return;
            }

            // 2. Probar objetos 3D (XRGrabInteractable, etc.)
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
            {
                _canInteract = true;
                if (Input.GetMouseButtonDown(0))
                    Debug.Log($"[EditorTestController] Clic en: {hit.collider.gameObject.name}");
            }
        }

        private bool TryHitUI(Ray ray)
        {
            // Usar PointerEventData para simular clic en UI World Space
            PointerEventData ped = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(Screen.width / 2f, Screen.height / 2f)
            };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current?.RaycastAll(ped, results);

            foreach (var r in results)
            {
                if (r.gameObject.GetComponent<Button>() != null ||
                    r.gameObject.GetComponent<IPointerClickHandler>() != null)
                    return true;
            }
            return results.Count > 0;
        }

        private void ClickUI(Ray ray)
        {
            PointerEventData ped = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(Screen.width / 2f, Screen.height / 2f)
            };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current?.RaycastAll(ped, results);

            foreach (var r in results)
            {
                ExecuteEvents.Execute(r.gameObject, ped, ExecuteEvents.pointerClickHandler);
                Button btn = r.gameObject.GetComponent<Button>();
                btn?.onClick.Invoke();
                Debug.Log($"[EditorTestController] Botón presionado: {r.gameObject.name}");
                break;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  Crosshair visual
        // ════════════════════════════════════════════════════════════════════

        private void DrawCrosshair()
        {
            float cx = Screen.width  / 2f;
            float cy = Screen.height / 2f;
            float size = 10f;
            float thick = 2f;

            GUI.color = _canInteract ? crosshairActive : crosshairInactive;

            // Horizontal
            GUI.DrawTexture(new Rect(cx - size, cy - thick / 2f, size * 2, thick), Texture2D.whiteTexture);
            // Vertical
            GUI.DrawTexture(new Rect(cx - thick / 2f, cy - size, thick, size * 2), Texture2D.whiteTexture);

            GUI.color = Color.white;
        }
    }
}
#endif
