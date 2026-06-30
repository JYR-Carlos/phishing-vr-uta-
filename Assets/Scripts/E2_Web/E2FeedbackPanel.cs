using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhishingVR.E2
{
    public class E2FeedbackPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text indicatorsText;
        [SerializeField] private Image panelBackground;
        [SerializeField] private float displayDuration = 5f;

        [SerializeField] private Color colorCorrect      = new(0.18f, 0.65f, 0.29f);
        [SerializeField] private Color colorIncorrect    = new(0.80f, 0.22f, 0.22f);
        [SerializeField] private Color colorFalsePositive = new(0.92f, 0.61f, 0.07f);

        private bool _dismissed;

        void Awake() => panel.SetActive(false);

        public IEnumerator Show(bool correct, bool falsePositive, SiteData site)
        {
            _dismissed = false;

            if (falsePositive)
            {
                headerText.text       = "Falso positivo";
                detailText.text       = "Este era un sitio legítimo. Reportarlo como phishing cuenta como error.";
                panelBackground.color = colorFalsePositive;
            }
            else if (correct)
            {
                headerText.text       = "¡Correcto!";
                detailText.text       = "Identificaste correctamente el sitio falsificado.";
                panelBackground.color = colorCorrect;
            }
            else
            {
                headerText.text       = "Sitio falsificado no detectado";
                detailText.text       = site.EducationalExplanation;
                panelBackground.color = colorIncorrect;
            }

            indicatorsText.text = BuildIndicatorsList(site);
            panel.SetActive(true);

            float elapsed = 0f;
            while (elapsed < displayDuration && !_dismissed)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            panel.SetActive(false);
        }

        public void Dismiss() => _dismissed = true;

        private static string BuildIndicatorsList(SiteData site)
        {
            if (!site.IsPhishing) return string.Empty;
            var sb = new StringBuilder("Indicadores presentes:\n");
            if ((site.ActiveIndicators & PhishingIndicator.BadURL) != 0)
                sb.AppendLine("• Dominio incorrecto en la URL");
            if ((site.ActiveIndicators & PhishingIndicator.NoHTTPS) != 0)
                sb.AppendLine("• Sin certificado HTTPS (candado ausente)");
            if ((site.ActiveIndicators & PhishingIndicator.TypoInLogo) != 0)
                sb.AppendLine("• Error tipográfico en el logotipo");
            if ((site.ActiveIndicators & PhishingIndicator.ExcessiveFormFields) != 0)
                sb.AppendLine("• Formulario solicita datos inusuales o excesivos");
            return sb.ToString();
        }
    }
}
