using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PhishingVR.E3
{
    /// <summary>
    /// Controla toda la UI del escenario de Smishing:
    ///   - Panel del SMS (oculto al inicio, visible cuando SmartphoneController lo activa)
    ///   - Btn_ClickSMSLink  → el usuario cayó en la trampa
    ///   - Btn_IgnoreSMS     → el usuario identificó el phishing
    ///
    /// Setup en Unity (ver guía SETUP_ESCENA_E3.md):
    ///   1. Agrega este script a un GameObject vacío llamado "E3UIController"
    ///   2. Crea el Canvas World Space dentro del modelo del teléfono
    ///   3. Arrastra las referencias en el Inspector
    /// </summary>
    public class E3UIController : MonoBehaviour
    {
        // ── Panel contenedor del SMS ─────────────────────────────────────────
        [Header("Panel SMS")]
        [Tooltip("El Panel que contiene el texto del SMS y los botones. Empieza desactivado.")]
        [SerializeField] private GameObject smsPanel;

        [Tooltip("Texto del remitente falso, ej: 'BANCO-CHILE'")]
        [SerializeField] private TextMeshProUGUI senderText;

        [Tooltip("Cuerpo del mensaje SMS de phishing")]
        [SerializeField] private TextMeshProUGUI smsBodyText;

        [Tooltip("Texto del link falso que aparece en el SMS")]
        [SerializeField] private TextMeshProUGUI linkPreviewText;

        // ── Botones de decisión ──────────────────────────────────────────────
        [Header("Botones")]
        [SerializeField] private Button btnClickSMSLink;   // rojo — trampa
        [SerializeField] private Button btnIgnoreSMS;      // verde — correcto

        // ── Referencia al manager principal ─────────────────────────────────
        [Header("Manager")]
        [SerializeField] private E3Manager e3Manager;

        // ── Contenido del SMS (editable desde el Inspector) ─────────────────
        [Header("Contenido del SMS phishing")]
        [SerializeField] private string fakeRemitente  = "BANCO-CHILE";
        [SerializeField] [TextArea] private string fakeMensaje =
            "Estimado cliente, detectamos actividad inusual en su cuenta.\n" +
            "Verifique sus datos de inmediato para evitar el bloqueo:\n";
        [SerializeField] private string fakeLinkTexto  = "http://banco-chile-seguro.tk/verificar";

        // ════════════════════════════════════════════════════════════════════
        //  Unity lifecycle
        // ════════════════════════════════════════════════════════════════════

        private void Awake()
        {
            // Conectar botones → callbacks
            if (btnClickSMSLink != null)
                btnClickSMSLink.onClick.AddListener(OnClickSMSLink);

            if (btnIgnoreSMS != null)
                btnIgnoreSMS.onClick.AddListener(OnIgnoreSMS);
        }

        private void Start()
        {
            // Asegurarse de que el panel empieza oculto
            HideButtons();
        }

        // ════════════════════════════════════════════════════════════════════
        //  API pública — llamada por SmartphoneController y E3Manager
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Activa el panel del SMS con el contenido de phishing.
        /// Llamado por SmartphoneController cuando el teléfono está a nivel de ojos.
        /// </summary>
        public void ShowSMS()
        {
            if (smsPanel == null) return;

            // Rellenar el texto
            if (senderText    != null) senderText.text    = fakeRemitente;
            if (smsBodyText   != null) smsBodyText.text   = fakeMensaje;
            if (linkPreviewText != null) linkPreviewText.text = fakeLinkTexto;

            smsPanel.SetActive(true);

            // Activar ambos botones
            if (btnClickSMSLink != null) btnClickSMSLink.gameObject.SetActive(true);
            if (btnIgnoreSMS    != null) btnIgnoreSMS.gameObject.SetActive(true);

            Debug.Log("[E3UIController] SMS de phishing revelado.");
        }

        /// <summary>
        /// Oculta el panel y deshabilita los botones.
        /// Llamado por E3Manager al inicio y al terminar el escenario.
        /// </summary>
        public void HideButtons()
        {
            if (smsPanel != null) smsPanel.SetActive(false);

            if (btnClickSMSLink != null) btnClickSMSLink.gameObject.SetActive(false);
            if (btnIgnoreSMS    != null) btnIgnoreSMS.gameObject.SetActive(false);
        }

        // ════════════════════════════════════════════════════════════════════
        //  Callbacks de botones
        // ════════════════════════════════════════════════════════════════════

        /// <summary>El usuario hizo clic en el link → cayó en la trampa.</summary>
        private void OnClickSMSLink()
        {
            Debug.Log("[E3UIController] Usuario hizo clic en el link falso.");
            DisableButtons();
            e3Manager?.OnUserDecision(clickedLink: true);
        }

        /// <summary>El usuario ignoró el SMS → identificó el phishing.</summary>
        private void OnIgnoreSMS()
        {
            Debug.Log("[E3UIController] Usuario ignoró el SMS.");
            DisableButtons();
            e3Manager?.OnUserDecision(clickedLink: false);
        }

        /// Desactiva los botones para evitar doble click
        private void DisableButtons()
        {
            if (btnClickSMSLink != null) btnClickSMSLink.interactable = false;
            if (btnIgnoreSMS    != null) btnIgnoreSMS.interactable    = false;
        }
    }
}
