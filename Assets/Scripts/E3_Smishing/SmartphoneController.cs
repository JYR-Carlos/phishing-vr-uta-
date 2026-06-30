using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace PhishingVR.E3
{
    /// <summary>
    /// Controla el smartphone virtual:
    ///   - Hereda de XRGrabInteractable para que el usuario pueda agarrarlo
    ///   - Detecta cuándo el teléfono está a nivel de los ojos del usuario (±0.25m)
    ///   - Cuando se cumple la condición, avisa a E3UIController para mostrar el SMS
    ///
    /// Setup en Unity:
    ///   1. Agrega este script al GameObject del teléfono (el que tiene el Mesh)
    ///   2. Asegúrate de que el GameObject también tenga XRGrabInteractable y Collider
    ///   3. Arrastra la referencia de E3UIController en el Inspector
    /// </summary>
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public class SmartphoneController : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Arrastra el objeto E3UIController desde la jerarquía")]
        [SerializeField] private E3UIController uiController;

        [Header("Configuración de detección")]
        [Tooltip("Distancia vertical máxima en metros desde los ojos para mostrar el SMS")]
        [SerializeField] private float eyeLevelThreshold = 0.25f;

        [Tooltip("Tiempo mínimo (segundos) a nivel de ojos antes de mostrar el SMS")]
        [SerializeField] private float timeToRevealSMS = 1.0f;

        // ── Estado interno ───────────────────────────────────────────────────
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grabInteractable;
        private bool _smsRevealed;
        private float _timeAtEyeLevel;
        private bool _isBeingHeld;
        private Camera _mainCamera;

        // ════════════════════════════════════════════════════════════════════
        //  Unity lifecycle
        // ════════════════════════════════════════════════════════════════════

        private void Awake()
        {
            _grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }

        private void OnEnable()
        {
            _grabInteractable.selectEntered.AddListener(OnGrabbed);
            _grabInteractable.selectExited.AddListener(OnReleased);
        }

        private void OnDisable()
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            _grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        private void Update()
        {
            if (!_isBeingHeld || _smsRevealed) return;

            // Buscar cámara principal (la cabeza del usuario en XR)
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_mainCamera == null) return;

            // Comparar altura Y del teléfono con la altura Y de los ojos
            float phoneY  = transform.position.y;
            float eyeY    = _mainCamera.transform.position.y;
            float delta   = Mathf.Abs(phoneY - eyeY);

            if (delta <= eyeLevelThreshold)
            {
                _timeAtEyeLevel += Time.deltaTime;

                if (_timeAtEyeLevel >= timeToRevealSMS)
                {
                    RevealSMS();
                }
            }
            else
            {
                // Si baja el teléfono, resetear el timer (pero no ocultar si ya se reveló)
                _timeAtEyeLevel = 0f;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  Eventos de XRGrabInteractable
        // ════════════════════════════════════════════════════════════════════

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            _isBeingHeld = true;
            Debug.Log("[SmartphoneController] Teléfono agarrado.");
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            _isBeingHeld = false;
            _timeAtEyeLevel = 0f;
            Debug.Log("[SmartphoneController] Teléfono soltado.");
        }

        // ════════════════════════════════════════════════════════════════════
        //  Lógica de revelación del SMS
        // ════════════════════════════════════════════════════════════════════

        private void RevealSMS()
        {
            _smsRevealed = true;
            Debug.Log("[SmartphoneController] Teléfono a nivel de ojos — mostrando SMS.");
            uiController?.ShowSMS();
        }

        // ════════════════════════════════════════════════════════════════════
        //  API pública
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Llamado por E3Manager al activar el escenario — resetea el estado.
        /// </summary>
        public void OnScenarioStart()
        {
            _smsRevealed    = false;
            _isBeingHeld    = false;
            _timeAtEyeLevel = 0f;
        }
    }
}
