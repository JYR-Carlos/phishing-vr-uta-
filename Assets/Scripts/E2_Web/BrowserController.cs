using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhishingVR.E2
{
    public class BrowserController : MonoBehaviour
    {
        // ── Barra de dirección ───────────────────────────────────────────────
        [Header("Barra de dirección")]
        [SerializeField] private TMP_Text  urlText;
        [SerializeField] private GameObject padlockValid;
        [SerializeField] private GameObject padlockInvalid;

        // ── Pestaña del navegador ────────────────────────────────────────────
        [Header("Pestaña")]
        [SerializeField] private TMP_Text browserTabTitle;

        // ── Header del sitio (barra de navegación del banco) ─────────────────
        [Header("Header del sitio")]
        [SerializeField] private Image    pageHeaderBackground;
        [SerializeField] private TMP_Text logoText;

        // ── Card de login ────────────────────────────────────────────────────
        [Header("Card de login")]
        [SerializeField] private TMP_Text pageSubtitle;
        [SerializeField] private Image    loginButtonImage;
        [SerializeField] private TMP_Text loginButtonLabel;

        // ── Formulario ───────────────────────────────────────────────────────
        [Header("Formulario")]
        [SerializeField] private TMP_Text      formFieldsText;
        [SerializeField] private RectTransform formFieldsContainer;
        [SerializeField] private Sprite        fieldBoxSprite;
        [SerializeField] private TMP_FontAsset fieldFont;
        [SerializeField] private GameObject    excessiveFieldsWarning;

        private readonly List<GameObject> _spawnedRows = new();

        // ════════════════════════════════════════════════════════════════════
        //  API pública
        // ════════════════════════════════════════════════════════════════════

        public void ShowSite(SiteData site)
        {
            // Dirección y candado
            if (urlText       != null) urlText.text = site.DisplayURL;
            if (padlockValid  != null) padlockValid.SetActive(site.HasHTTPS);
            if (padlockInvalid!= null) padlockInvalid.SetActive(!site.HasHTTPS);

            // Nombres del sitio
            if (logoText       != null) logoText.text        = site.LogoText;
            if (browserTabTitle!= null) browserTabTitle.text = site.DisplayName;

            // Campos excesivos
            bool excessive = (site.ActiveIndicators & PhishingIndicator.ExcessiveFormFields) != 0;
            if (excessiveFieldsWarning != null) excessiveFieldsWarning.SetActive(excessive);

            // Formulario
            if (formFieldsContainer != null)
                BuildFormBoxes(site.FormFieldLabels);
            else if (formFieldsText != null)
                formFieldsText.text = string.Join("\n", site.FormFieldLabels);

            // Paleta de colores según el sitio
            ApplyTheme(site);
        }

        // ════════════════════════════════════════════════════════════════════
        //  Tema visual por sitio
        // ════════════════════════════════════════════════════════════════════

        private void ApplyTheme(SiteData site)
        {
            bool isFacebook = site.LogoText.ToLower().Contains("facebook")
                           || site.SiteId.Contains("social")
                           || site.DisplayURL.ToLower().Contains("faceb");

            Color headerColor, btnColor;
            string loginLabel, subtitle;

            if (isFacebook)
            {
                headerColor = new Color(0.094f, 0.467f, 0.945f); // #1877F2
                btnColor    = new Color(0.094f, 0.467f, 0.945f);
                loginLabel  = "Iniciar sesión";
                subtitle    = "Inicia sesión en Facebook";
            }
            else // BancoEstado real o falso
            {
                headerColor = new Color(0.106f, 0.227f, 0.420f); // #1B3A6B azul oscuro BancoEstado
                btnColor    = new Color(0.961f, 0.510f, 0.122f); // #F5821F naranja BancoEstado
                loginLabel  = "Ingresar";
                subtitle    = "Ingresa a tu cuenta";
            }

            if (pageHeaderBackground != null) pageHeaderBackground.color = headerColor;
            if (loginButtonImage     != null) loginButtonImage.color     = btnColor;
            if (loginButtonLabel     != null) loginButtonLabel.text      = loginLabel;
            if (pageSubtitle         != null) pageSubtitle.text          = subtitle;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Formulario dinámico
        // ════════════════════════════════════════════════════════════════════

        private void BuildFormBoxes(string[] labels)
        {
            foreach (var go in _spawnedRows)
                if (go != null) Destroy(go);
            _spawnedRows.Clear();

            if (labels == null) return;

            const float rowH = 54f, gap = 8f;
            float y = -4f;
            foreach (string label in labels)
            {
                _spawnedRows.Add(CreateFieldRow(label, y, rowH));
                y -= rowH + gap;
            }
        }

        private GameObject CreateFieldRow(string label, float y, float rowH)
        {
            var row = new GameObject($"Field_{label}", typeof(RectTransform));
            var rrt = (RectTransform)row.transform;
            rrt.SetParent(formFieldsContainer, false);
            rrt.anchorMin = new Vector2(0f, 1f);
            rrt.anchorMax = new Vector2(1f, 1f);
            rrt.pivot     = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0f, y);
            rrt.sizeDelta        = new Vector2(0f, rowH);

            // Etiqueta encima del campo
            var lbl = NewTMP(rrt, "Label", label, 13f,
                new Color(0.35f, 0.38f, 0.43f), TextAlignmentOptions.MidlineLeft);
            var lrt = lbl.rectTransform;
            lrt.anchorMin = new Vector2(0f, 0.5f);
            lrt.anchorMax = new Vector2(1f, 1f);
            lrt.offsetMin = new Vector2(2f, 0f);
            lrt.offsetMax = Vector2.zero;

            // Caja del campo
            var box = new GameObject("InputBox", typeof(RectTransform), typeof(Image));
            var brt = (RectTransform)box.transform;
            brt.SetParent(rrt, false);
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 0.50f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;

            var img = box.GetComponent<Image>();
            img.color = new Color(0.97f, 0.97f, 0.98f);
            if (fieldBoxSprite != null) { img.sprite = fieldBoxSprite; img.type = Image.Type.Sliced; }

            return row;
        }

        private TextMeshProUGUI NewTMP(Transform parent, string name, string text,
                                       float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text      = text;
            t.fontSize  = size;
            t.color     = color;
            t.alignment = align;
            t.raycastTarget = false;
            if (fieldFont != null) t.font = fieldFont;
            return t;
        }
    }
}
