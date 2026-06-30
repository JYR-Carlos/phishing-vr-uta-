using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhishingVR.E2
{
    public class BrowserController : MonoBehaviour
    {
        [SerializeField] private TMP_Text urlText;
        [SerializeField] private GameObject padlockValid;
        [SerializeField] private GameObject padlockInvalid;
        [SerializeField] private TMP_Text logoText;
        [SerializeField] private TMP_Text formFieldsText;
        [SerializeField] private GameObject excessiveFieldsWarning;

        [Header("Formulario con cajas (opcional — lo cablea el generador de texturas)")]
        [Tooltip("Si se asigna, el formulario se dibuja como cajas de input en vez de texto plano.")]
        [SerializeField] private RectTransform formFieldsContainer;
        [SerializeField] private Sprite fieldBoxSprite;
        [SerializeField] private TMP_FontAsset fieldFont;

        private readonly List<GameObject> _spawnedRows = new();

        public void ShowSite(SiteData site)
        {
            urlText.text = site.DisplayURL;
            padlockValid.SetActive(site.HasHTTPS);
            padlockInvalid.SetActive(!site.HasHTTPS);
            logoText.text = site.LogoText;

            bool excessive = (site.ActiveIndicators & PhishingIndicator.ExcessiveFormFields) != 0;
            excessiveFieldsWarning.SetActive(excessive);

            if (formFieldsContainer != null)
                BuildFormBoxes(site.FormFieldLabels);
            else if (formFieldsText != null)
                formFieldsText.text = string.Join("\n", site.FormFieldLabels);
        }

        // ── Formulario dinámico ──────────────────────────────────────────────

        private void BuildFormBoxes(string[] labels)
        {
            foreach (var go in _spawnedRows)
                if (go != null) Destroy(go);
            _spawnedRows.Clear();

            if (labels == null) return;

            const float rowH = 56f, spacing = 12f;
            float y = -4f;
            foreach (string label in labels)
            {
                _spawnedRows.Add(CreateFieldRow(label, y, rowH));
                y -= rowH + spacing;
            }
        }

        private GameObject CreateFieldRow(string label, float y, float rowH)
        {
            // Fila (anclada arriba, ancho completo del contenedor)
            var row = new GameObject($"Field_{label}", typeof(RectTransform));
            var rrt = (RectTransform)row.transform;
            rrt.SetParent(formFieldsContainer, false);
            rrt.anchorMin = new Vector2(0f, 1f);
            rrt.anchorMax = new Vector2(1f, 1f);
            rrt.pivot = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0f, y);
            rrt.sizeDelta = new Vector2(-16f, rowH);

            // Etiqueta encima de la caja
            var lbl = NewText(rrt, "Label", label, 15, new Color(0.22f, 0.24f, 0.28f),
                              TextAlignmentOptions.Left);
            var lrt = lbl.rectTransform;
            lrt.anchorMin = new Vector2(0f, 1f);
            lrt.anchorMax = new Vector2(1f, 1f);
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.anchoredPosition = new Vector2(4f, 0f);
            lrt.sizeDelta = new Vector2(-8f, 20f);

            // Caja del input (parte inferior de la fila)
            var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
            var brt = (RectTransform)box.transform;
            brt.SetParent(rrt, false);
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 0f);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(0f, rowH - 24f);

            var img = box.GetComponent<Image>();
            img.color = new Color(0.97f, 0.97f, 0.98f);   // gris muy claro, look de input
            if (fieldBoxSprite != null)
            {
                img.sprite = fieldBoxSprite;
                img.type = Image.Type.Sliced;
            }

            return row;
        }

        private TextMeshProUGUI NewText(Transform parent, string name, string text,
                                        float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            if (fieldFont != null) t.font = fieldFont;
            return t;
        }
    }
}
