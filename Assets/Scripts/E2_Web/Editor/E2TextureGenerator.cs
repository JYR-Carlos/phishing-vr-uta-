// Este script solo existe en el Editor de Unity — NO se incluye en la build final.
//
// Dos menús (Tools → PhishingVR → ...):
//   • "Generar Texturas UI E2"            → solo crea los sprites en Assets/Data/E2/Textures/
//   • "Aplicar Texturas + Materiales E2"  → crea los sprites Y los materiales de la sala,
//                                            y los asigna automáticamente a la escena abierta.
//
// Sprites generados:
//   • UI_RoundedRect   → rectángulo redondeado (9-slice) para paneles, barras y botones
//   • UI_PageGradient  → fondo con degradado suave para el cuerpo de la página
//   • UI_LogoBadge     → cuadrado redondeado con degradado para el logo
//
// Materiales generados (URP Lit) en Assets/Data/E2/Materials/:
//   • Mat_Floor (gris) · Mat_Desk (madera) · Mat_Wall (claro)
//
// Todo lo aplicado a la escena se puede revertir con Ctrl+Z. Recuerda guardar (Ctrl+S).

#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PhishingVR.E2.Editor
{
    public static class E2TextureGenerator
    {
        private const string TexFolder = "Assets/Data/E2/Textures";
        private const string MatFolder = "Assets/Data/E2/Materials";

        // ════════════════════════════════════════════════════════════════════
        //  Menús
        // ════════════════════════════════════════════════════════════════════

        [MenuItem("Tools/PhishingVR/Generar Texturas UI E2")]
        public static void GenerateTexturesOnly()
        {
            GenerateTextures();
            EditorUtility.DisplayDialog("PhishingVR — Texturas generadas",
                "3 sprites creados en Assets/Data/E2/Textures/.\n\n" +
                "Asígnalos a mano en 'Source Image', o usa\n" +
                "Tools → PhishingVR → Aplicar Texturas + Materiales E2\n" +
                "para que se asignen solos.", "OK");
        }

        [MenuItem("Tools/PhishingVR/Aplicar Texturas + Materiales E2")]
        public static void GenerateAndApply()
        {
            GenerateTextures();
            GenerateMaterials();

            int changes = ApplyToScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("PhishingVR — Aplicado a la escena",
                changes > 0
                    ? $"Listo. Se asignaron texturas/materiales a {changes} elementos.\n\n" +
                      "Revisa cómo quedó y guarda con Ctrl+S.\n" +
                      "Si algo no te gusta: Ctrl+Z para deshacer."
                    : "No se encontraron los GameObjects de la escena E2.\n\n" +
                      "Abre primero la escena E2_Web.unity y vuelve a ejecutarlo.",
                "OK");
        }

        // ════════════════════════════════════════════════════════════════════
        //  Generación de assets
        // ════════════════════════════════════════════════════════════════════

        private static void GenerateTextures()
        {
            EnsureFolder("Assets/Data/E2", "Textures");

            int rounded = 64, radius = 22;
            SavePng(MakeRoundedRect(rounded, rounded, radius, Color.white), TexFolder, "UI_RoundedRect");
            ApplySpriteImport($"{TexFolder}/UI_RoundedRect.png", radius);

            SavePng(MakeVerticalGradient(8, 256, new Color(0.93f, 0.95f, 0.98f), Color.white),
                    TexFolder, "UI_PageGradient");
            ApplySpriteImport($"{TexFolder}/UI_PageGradient.png", 0);

            SavePng(MakeRoundedGradient(128, 128, 28,
                        new Color(0.10f, 0.40f, 0.80f), new Color(0.05f, 0.20f, 0.50f)),
                    TexFolder, "UI_LogoBadge");
            ApplySpriteImport($"{TexFolder}/UI_LogoBadge.png", 0);
        }

        private static void GenerateMaterials()
        {
            EnsureFolder("Assets/Data/E2", "Materials");
            MakeMaterial("Mat_Floor", new Color(0.50f, 0.51f, 0.53f), 0.15f);
            MakeMaterial("Mat_Desk",  new Color(0.45f, 0.30f, 0.18f), 0.25f);
            MakeMaterial("Mat_Wall",  new Color(0.90f, 0.90f, 0.92f), 0.05f);
        }

        // ════════════════════════════════════════════════════════════════════
        //  Aplicación a la escena
        // ════════════════════════════════════════════════════════════════════

        private static int ApplyToScene()
        {
            Sprite rounded  = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexFolder}/UI_RoundedRect.png");
            Sprite gradient = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexFolder}/UI_PageGradient.png");
            Sprite badge    = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexFolder}/UI_LogoBadge.png");

            int n = 0;
            n += SetImage("BG_Panel",          gradient, Image.Type.Simple);
            n += SetImage("BrowserBar",        rounded,  Image.Type.Sliced);
            n += SetImage("BrowserButtons",    rounded,  Image.Type.Sliced);
            n += SetImage("Btn_IngresarDatos", rounded,  Image.Type.Sliced);
            n += SetImage("Btn_ReportarSitio", rounded,  Image.Type.Sliced);
            n += SetImage("Btn_Dismiss",       rounded,  Image.Type.Sliced);
            n += SetImage("Feedback86",        rounded,  Image.Type.Sliced);

            n += AddLogoBadge(badge);
            n += WireForm(rounded);

            n += SetMaterial("E2_Floor", "Mat_Floor");
            n += SetMaterial("E2_Desk",  "Mat_Desk");
            n += SetMaterial("E2_Wall_Back", "Mat_Wall");

            if (n > 0)
                EditorSceneManager.MarkAllScenesDirty();
            return n;
        }

        private static int SetImage(string goName, Sprite sprite, Image.Type type)
        {
            if (sprite == null) return 0;
            var img = FindInScene<Image>(goName);
            if (img == null) return 0;

            Undo.RecordObject(img, "Aplicar textura E2");
            img.sprite = sprite;
            img.type = type;
            if (img.color.a < 0.99f)                       // los fondos translúcidos quedaban "lavados"
                img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);
            EditorUtility.SetDirty(img);
            return 1;
        }

        // Crea un Image con el badge justo detrás del texto del logo (mismo rect).
        private static int AddLogoBadge(Sprite badge)
        {
            if (badge == null) return 0;
            var logo = FindInScene<TMP_Text>("TMP_Logo");
            if (logo == null) return 0;

            var parent = logo.transform.parent as RectTransform;
            var logoRt = logo.transform as RectTransform;
            if (parent == null || logoRt == null) return 0;

            // Evitar duplicados si se ejecuta dos veces.
            var existing = parent.Find("Logo_Badge");
            if (existing != null) return 0;

            var go = new GameObject("Logo_Badge", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Crear logo badge");
            go.layer = logo.gameObject.layer;

            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = logoRt.anchorMin;
            rt.anchorMax = logoRt.anchorMax;
            rt.anchoredPosition = logoRt.anchoredPosition;
            rt.sizeDelta = logoRt.sizeDelta;
            rt.pivot = logoRt.pivot;
            rt.SetSiblingIndex(logoRt.GetSiblingIndex());  // queda detrás del texto

            var img = go.GetComponent<Image>();
            img.sprite = badge;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            return 1;
        }

        // Cablea el formulario dinámico del BrowserController: le pasa el sprite de
        // las cajas, crea un contenedor donde estaba el texto plano y lo oculta.
        private static int WireForm(Sprite boxSprite)
        {
            var bc = FindFirstInScene<BrowserController>();
            if (bc == null) return 0;

            var so = new SerializedObject(bc);
            var pBox = so.FindProperty("fieldBoxSprite");
            var pContainer = so.FindProperty("formFieldsContainer");
            if (pBox == null || pContainer == null) return 0;

            pBox.objectReferenceValue = boxSprite;

            if (pContainer.objectReferenceValue == null)
            {
                var formText = FindInScene<TMP_Text>("TMP_FormFields");
                if (formText != null && formText.transform is RectTransform formRt
                    && formRt.parent is RectTransform parent)
                {
                    var go = new GameObject("FormFieldsContainer", typeof(RectTransform));
                    Undo.RegisterCreatedObjectUndo(go, "Crear contenedor de formulario");
                    go.layer = formText.gameObject.layer;

                    var rt = (RectTransform)go.transform;
                    rt.SetParent(parent, false);
                    rt.anchorMin = formRt.anchorMin;
                    rt.anchorMax = formRt.anchorMax;
                    rt.pivot = formRt.pivot;
                    rt.anchoredPosition = formRt.anchoredPosition;
                    rt.sizeDelta = formRt.sizeDelta;
                    rt.SetSiblingIndex(formRt.GetSiblingIndex());

                    pContainer.objectReferenceValue = rt;

                    Undo.RecordObject(formText.gameObject, "Ocultar texto de campos");
                    formText.gameObject.SetActive(false);  // se reemplaza por las cajas
                }
            }

            so.ApplyModifiedProperties();   // registra Undo automáticamente
            EditorUtility.SetDirty(bc);
            return 1;
        }

        private static int SetMaterial(string goName, string matName)
        {
            var rend = FindInScene<MeshRenderer>(goName);
            if (rend == null) return 0;
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MatFolder}/{matName}.mat");
            if (mat == null) return 0;

            Undo.RecordObject(rend, "Aplicar material E2");
            rend.sharedMaterial = mat;
            EditorUtility.SetDirty(rend);
            return 1;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Generadores de píxeles
        // ════════════════════════════════════════════════════════════════════

        private static Texture2D MakeRoundedRect(int w, int h, int r, Color color)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float a = CornerAlpha(x, y, w, h, r);
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
                }
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeRoundedGradient(int w, int h, int r, Color top, Color bottom)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                Color c = Color.Lerp(bottom, top, (float)y / (h - 1));
                for (int x = 0; x < w; x++)
                {
                    float a = CornerAlpha(x, y, w, h, r);
                    tex.SetPixel(x, y, new Color(c.r, c.g, c.b, a));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeVerticalGradient(int w, int h, Color top, Color bottom)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                Color c = Color.Lerp(bottom, top, (float)y / (h - 1));
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }

        private static float CornerAlpha(int x, int y, int w, int h, int r)
        {
            float cx = Mathf.Clamp(x, r, w - 1 - r);
            float cy = Mathf.Clamp(y, r, h - 1 - r);
            float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            return Mathf.Clamp01(r - dist + 0.5f);
        }

        // ════════════════════════════════════════════════════════════════════
        //  Helpers
        // ════════════════════════════════════════════════════════════════════

        // Busca un componente por nombre de GameObject en la escena abierta,
        // incluyendo objetos DESACTIVOS (como Feedback86) y excluyendo prefabs/assets.
        private static T FindInScene<T>(string goName) where T : Component
        {
            foreach (var c in Resources.FindObjectsOfTypeAll<T>())
            {
                if (c.gameObject.name != goName) continue;
                if (EditorUtility.IsPersistent(c.gameObject)) continue;
                if (!c.gameObject.scene.IsValid()) continue;
                return c;
            }
            return null;
        }

        // Primer componente de un tipo en la escena abierta (ignora nombre, prefabs y assets).
        private static T FindFirstInScene<T>() where T : Component
        {
            foreach (var c in Resources.FindObjectsOfTypeAll<T>())
            {
                if (EditorUtility.IsPersistent(c.gameObject)) continue;
                if (!c.gameObject.scene.IsValid()) continue;
                return c;
            }
            return null;
        }

        private static void MakeMaterial(string name, Color color, float smoothness)
        {
            string path = $"{MatFolder}/{name}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            else if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

            AssetDatabase.CreateAsset(mat, path);
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(parent))
                AssetDatabase.CreateFolder("Assets/Data", Path.GetFileName(parent));
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static void SavePng(Texture2D tex, string folder, string name)
        {
            File.WriteAllBytes(Path.Combine(Application.dataPath,
                folder.Substring("Assets/".Length) + $"/{name}.png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ApplySpriteImport(string assetPath, int border)
        {
            AssetDatabase.ImportAsset(assetPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;

            if (border > 0)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteBorder = new Vector4(border, border, border, border);
                importer.SetTextureSettings(settings);
            }
            importer.SaveAndReimport();
        }
    }
}
#endif
