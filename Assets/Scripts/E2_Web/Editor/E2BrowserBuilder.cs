// E2BrowserBuilder.cs — Solo Editor, NO entra en la build.
//
// Menú: Tools → PhishingVR → E2 Navegador → Reconstruir UI Completo
//
// Diseñado para correr en "Demo 2 - Office Set 1.unity".
// Selecciona el monitor en la jerarquía ANTES de correr el menú.
//
// Layout generado (1080 × 810 px World Space):
//
//  ┌─ Tab bar ──────────────────────────── [×] ─┐  ← Btn_CerrarPestana
//  ├─ [← → ↺] [🔒 http://bancestado-cl.tk/...] ─┤
//  ├────────────────────────────────────────────-┤
//  │  [BancEstado]  Mis Cuentas  Transferencias  │  ← header azul #1B3A6B
//  ├─────────────────────────────────────────────┤
//  │      ┌────────────────────────────┐         │
//  │      │  Ingresa a tu cuenta       │         │
//  │      │  RUT    [______________]   │         │
//  │      │  Clave  [______________]   │         │
//  │      │  ⚠ campos excesivos        │         │
//  │      │  [       Ingresar        ] │ ← Btn_IngresarDatos
//  │      │  ¿Olvidaste tu clave?      │         │
//  │      └────────────────────────────┘         │
//  ├─────────────────────────────────────────────┤
//  │     © BancoEstado Chile  |  Seguridad  ...  │
//  └─────────────────────────────────────────────┘

#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace PhishingVR.E2.Editor
{
    public static class E2BrowserBuilder
    {
        private const string BrowserCanvasName  = "E2_BrowserCanvas";
        private const string FeedbackCanvasName = "E2_FeedbackCanvas";
        private const string SiteDataFolder     = "Assets/Data/E2";

        // Paleta BancoEstado
        static readonly Color BancoBlueDark = new(0.106f, 0.227f, 0.420f); // #1B3A6B
        static readonly Color BancoBlueMid  = new(0.157f, 0.337f, 0.596f); // #284F98
        static readonly Color BancoOrange   = new(0.961f, 0.510f, 0.122f); // #F5821F
        static readonly Color ChromeDark    = new(0.173f, 0.180f, 0.196f); // #2C2E32
        static readonly Color ChromeMid     = new(0.235f, 0.247f, 0.263f); // #3C3F43
        static readonly Color PageBg        = new(0.961f, 0.965f, 0.973f); // #F5F6F8
        static readonly Color TextDark      = new(0.133f, 0.145f, 0.165f);
        static readonly Color TextMid       = new(0.380f, 0.400f, 0.427f);
        static readonly Color TextLight     = new(0.600f, 0.615f, 0.635f);
        static readonly Color WarnYellow    = new(1.000f, 0.843f, 0.094f);
        static readonly Color WarnText      = new(0.333f, 0.255f, 0f);

        // ════════════════════════════════════════════════════════════════════
        //  Menú principal
        // ════════════════════════════════════════════════════════════════════

        [MenuItem("Tools/PhishingVR/E2 Navegador/Reconstruir UI Completo")]
        public static void RebuildAll()
        {
            var browserGo = GetOrCreateCanvas(BrowserCanvasName, new Vector2(1080f, 810f), 0.001f);
            AnchorToSelectedMonitor(browserGo);
            ClearChildren(browserGo);

            BuildBrowserUI(browserGo,
                out var urlTMP,       out var padValid,     out var padInvalid,
                out var tabTitleTMP,  out var logoTMP,      out var pageHeaderBg,
                out var subtitleTMP,  out var loginBtnImg,  out var loginBtnLbl,
                out var formTMP,      out var formCont,     out var excessWarn,
                out var btnClose,     out var btnEnter);

            var feedbackGo = GetOrCreateCanvas(FeedbackCanvasName, new Vector2(680f, 460f), 0.0018f);
            PlaceFeedback(feedbackGo);
            ClearChildren(feedbackGo);

            BuildFeedbackUI(feedbackGo,
                out var fbPanel, out var fbHeader, out var fbDetail,
                out var fbIndicators, out var fbBg);

            WireAll(browserGo, feedbackGo,
                urlTMP, padValid, padInvalid, tabTitleTMP, logoTMP, pageHeaderBg,
                subtitleTMP, loginBtnImg, loginBtnLbl, formTMP, formCont, excessWarn,
                btnClose, btnEnter,
                fbPanel, fbHeader, fbDetail, fbIndicators, fbBg);

            MarkDirty();
            EditorUtility.DisplayDialog("PhishingVR — E2 UI lista",
                "UI reconstruida con estilo BancoEstado.\n\n" +
                "• [×] en la pestaña = 'Cerrar pestaña'\n" +
                "• [Ingresar] en la card = 'Ingresar datos'\n\n" +
                "Guarda con Ctrl+S.", "OK");
        }

        // ════════════════════════════════════════════════════════════════════
        //  Browser Canvas
        // ════════════════════════════════════════════════════════════════════

        static void BuildBrowserUI(
            GameObject    root,
            out TMP_Text       urlTMP,
            out GameObject     padValid,
            out GameObject     padInvalid,
            out TMP_Text       tabTitleTMP,
            out TMP_Text       logoTMP,
            out Image          pageHeaderBg,
            out TMP_Text       subtitleTMP,
            out Image          loginBtnImg,
            out TMP_Text       loginBtnLbl,
            out TMP_Text       formTMP,
            out RectTransform  formCont,
            out GameObject     excessWarn,
            out Button         btnClose,
            out Button         btnEnter)
        {
            // Fondo general de página
            MakeImage(root.transform, "BG_Page", PageBg, Anchors(0,0,1,1));

            // ── TAB BAR (top 4.5%) ───────────────────────────────────────────
            var tabBar = MakeImage(root.transform, "TabBar", ChromeDark, Anchors(0f,0.955f,1f,1f));

            var tab = MakeImage(tabBar.transform, "Tab_Active", ChromeMid, null);
            SetAnchors(tab.GetComponent<RectTransform>(), 0f,0f, 0.62f,1f);
            tab.GetComponent<RectTransform>().offsetMin = new Vector2(4f, 2f);

            // Título de la pestaña
            tabTitleTMP = MakeTMP(tab.transform, "TabTitle",
                "BancEstado - Iniciar sesión", 11f,
                new Color(0.85f, 0.87f, 0.90f), TextAlignmentOptions.MidlineLeft);
            SetAnchors(tabTitleTMP.rectTransform, 0f,0f, 1f,1f);
            tabTitleTMP.rectTransform.offsetMin = new Vector2(6f,  0f);
            tabTitleTMP.rectTransform.offsetMax = new Vector2(-24f, 0f);

            // Botón × (= Btn_CerrarPestana)
            btnClose = MakeButton(tab.transform, "Btn_CerrarPestana", "×",
                new Color(0.48f,0.50f,0.53f), 15f);
            SetAnchors(btnClose.GetComponent<RectTransform>(), 1f,0.1f, 1f,0.9f);
            btnClose.GetComponent<RectTransform>().offsetMin = new Vector2(-22f, 0f);
            btnClose.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            // ── TOOLBAR (siguiente 6%) ───────────────────────────────────────
            var toolbar = MakeImage(root.transform, "Toolbar", ChromeMid, Anchors(0f,0.895f,1f,0.955f));

            // Botones nav decorativos
            var navLbl = MakeTMP(toolbar.transform, "NavBtns", "←  →  ↺",
                14f, TextLight, TextAlignmentOptions.MidlineLeft);
            SetAnchors(navLbl.rectTransform, 0f,0f, 0f,1f);
            navLbl.rectTransform.offsetMin = new Vector2(6f, 0f);
            navLbl.rectTransform.offsetMax = new Vector2(90f, 0f);

            // Caja de dirección
            var addrBox = MakeImage(toolbar.transform, "AddressBox",
                new Color(0.145f,0.153f,0.165f), null);
            SetAnchors(addrBox.GetComponent<RectTransform>(), 0f,0f, 1f,1f);
            addrBox.GetComponent<RectTransform>().offsetMin = new Vector2(96f, 4f);
            addrBox.GetComponent<RectTransform>().offsetMax = new Vector2(-36f,-4f);

            // Candado válido
            padValid = MakeImage(addrBox.transform, "Padlock_Valid",
                new Color(0.18f,0.68f,0.32f), null).gameObject;
            var pvRt = padValid.GetComponent<RectTransform>();
            pvRt.anchorMin = new Vector2(0f,0f);
            pvRt.anchorMax = new Vector2(0f,1f);
            pvRt.offsetMin = new Vector2(4f,4f);
            pvRt.offsetMax = new Vector2(26f,-4f);
            MakeTMP(padValid.transform, "Icon", "🔒", 11f, Color.white, TextAlignmentOptions.Center);

            // Candado inválido
            padInvalid = MakeImage(addrBox.transform, "Padlock_Invalid",
                new Color(0.82f,0.26f,0.14f), null).gameObject;
            var piRt = padInvalid.GetComponent<RectTransform>();
            piRt.anchorMin = new Vector2(0f,0f);
            piRt.anchorMax = new Vector2(0f,1f);
            piRt.offsetMin = new Vector2(4f,4f);
            piRt.offsetMax = new Vector2(26f,-4f);
            MakeTMP(padInvalid.transform, "Icon", "⚠", 11f, Color.white, TextAlignmentOptions.Center);
            padInvalid.SetActive(false);

            // Texto URL
            urlTMP = MakeTMP(addrBox.transform, "TMP_URL",
                "http://bancestado-cl.tk/login", 13f, TextLight, TextAlignmentOptions.MidlineLeft);
            SetAnchors(urlTMP.rectTransform, 0f,0f, 1f,1f);
            urlTMP.rectTransform.offsetMin = new Vector2(30f,0f);
            urlTMP.rectTransform.offsetMax = new Vector2(-4f,0f);

            // ── SITE HEADER (8% siguiente) ───────────────────────────────────
            var siteHeader = MakeImage(root.transform, "SiteHeader", BancoBlueDark,
                Anchors(0f,0.817f,1f,0.895f));
            pageHeaderBg = siteHeader.GetComponent<Image>();

            logoTMP = MakeTMP(siteHeader.transform, "TMP_Logo",
                "BancEstado", 22f, Color.white, TextAlignmentOptions.MidlineLeft);
            logoTMP.fontStyle = FontStyles.Bold;
            SetAnchors(logoTMP.rectTransform, 0f,0f, 0.28f,1f);
            logoTMP.rectTransform.offsetMin = new Vector2(18f,0f);

            // Links de navegación decorativos
            string[] links = { "Mis Cuentas", "Transferencias", "Créditos", "BancaEmpresas" };
            float nx = 0.29f;
            foreach (var lnk in links)
            {
                float nxEnd = nx + 0.16f;
                var lt = MakeTMP(siteHeader.transform, $"Nav_{lnk}", lnk,
                    12f, new Color(0.82f,0.88f,0.98f), TextAlignmentOptions.Center);
                SetAnchors(lt.rectTransform, nx,0f, nxEnd,1f);
                nx = nxEnd;
            }

            // ── BODY (del 7% al 81.7%) ──────────────────────────────────────
            var pageBody = MakeImage(root.transform, "PageBody", PageBg,
                Anchors(0f,0.07f,1f,0.817f));

            // Card de login centrada
            var card = MakeImage(pageBody.transform, "LoginCard", Color.white, null);
            SetAnchors(card.GetComponent<RectTransform>(), 0.22f,0.04f, 0.78f,0.98f);

            // Franja de color en el tope de la card
            MakeImage(card.transform, "CardTopBar", BancoBlueDark, Anchors(0f,0.92f,1f,1f));

            // Título de la card
            subtitleTMP = MakeTMP(card.transform, "CardTitle",
                "Ingresa a tu cuenta", 18f, TextDark, TextAlignmentOptions.Center);
            subtitleTMP.fontStyle = FontStyles.Bold;
            SetAnchors(subtitleTMP.rectTransform, 0.04f,0.80f, 0.96f,0.92f);

            // Separador
            MakeImage(card.transform, "TitleSep",
                new Color(0.88f,0.89f,0.91f), Anchors(0.04f,0.793f,0.96f,0.797f));

            // Aviso de campos excesivos (oculto por defecto)
            excessWarn = new GameObject("ExcessiveFieldsWarning",
                typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(excessWarn, "E2Build");
            excessWarn.transform.SetParent(card.transform, false);
            excessWarn.GetComponent<Image>().color = WarnYellow;
            SetAnchors(excessWarn.GetComponent<RectTransform>(), 0.04f,0.395f, 0.96f,0.445f);
            var ewLbl = excessWarn.AddComponent<TextMeshProUGUI>();
            ewLbl.text = "⚠  Este formulario solicita datos inusuales o excesivos";
            ewLbl.fontSize = 11.5f;
            ewLbl.color = WarnText;
            ewLbl.alignment = TextAlignmentOptions.Center;
            ewLbl.raycastTarget = false;
            excessWarn.SetActive(false);

            // Contenedor dinámico de campos
            var fcGo = new GameObject("FormFieldsContainer", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(fcGo, "E2Build");
            fcGo.transform.SetParent(card.transform, false);
            formCont = fcGo.GetComponent<RectTransform>();
            SetAnchors(formCont, 0.06f,0.44f, 0.94f,0.80f);

            // Fallback texto (oculto)
            formTMP = MakeTMP(card.transform, "TMP_FormFields", "", 13f, TextDark);
            formTMP.gameObject.SetActive(false);

            // Botón Ingresar dentro de la card (= Btn_IngresarDatos)
            btnEnter = MakeButton(card.transform, "Btn_IngresarDatos", "Ingresar",
                BancoOrange, 16f);
            SetAnchors(btnEnter.GetComponent<RectTransform>(), 0.10f,0.285f, 0.90f,0.390f);
            loginBtnImg = btnEnter.GetComponent<Image>();
            loginBtnLbl = btnEnter.GetComponentInChildren<TMP_Text>();

            // ¿Olvidaste tu clave? (decorativo)
            var forgot = MakeTMP(card.transform, "ForgotLink",
                "¿Olvidaste tu clave?", 12f, BancoBlueMid, TextAlignmentOptions.Center);
            SetAnchors(forgot.rectTransform, 0.08f,0.21f, 0.92f,0.28f);

            // Separador
            MakeImage(card.transform, "Divider",
                new Color(0.88f,0.89f,0.91f), Anchors(0.04f,0.195f,0.96f,0.199f));

            // Badge de seguridad
            var sec = MakeTMP(card.transform, "SecurityBadge",
                "🔒  Conexión segura SSL  |  BancoEstado protege tus datos",
                10f, TextMid, TextAlignmentOptions.Center);
            SetAnchors(sec.rectTransform, 0.04f,0.12f, 0.96f,0.195f);

            // Logos de confianza
            var trust = MakeTMP(card.transform, "TrustLogos",
                "VISA    Mastercard    Redbanc    SBIF    CMF",
                10f, TextLight, TextAlignmentOptions.Center);
            SetAnchors(trust.rectTransform, 0.04f,0.03f, 0.96f,0.12f);

            // ── FOOTER (7% inferior) ─────────────────────────────────────────
            var footer = MakeImage(root.transform, "PageFooter",
                new Color(0.13f,0.14f,0.16f), Anchors(0f,0f,1f,0.07f));
            var ft = MakeTMP(footer.transform, "FooterText",
                "© BancoEstado Chile S.A. 2024   |   Política de Privacidad   |   Seguridad   |   Contacto",
                10f, TextLight, TextAlignmentOptions.Center);
            SetAnchors(ft.rectTransform, 0f,0f, 1f,1f);
        }

        // ════════════════════════════════════════════════════════════════════
        //  Feedback Canvas
        // ════════════════════════════════════════════════════════════════════

        static void BuildFeedbackUI(
            GameObject   root,
            out GameObject fbPanel,
            out TMP_Text   fbHeader,
            out TMP_Text   fbDetail,
            out TMP_Text   fbIndicators,
            out Image      fbBg)
        {
            var panelGo = new GameObject("FeedbackPanel", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(panelGo, "E2Build");
            panelGo.transform.SetParent(root.transform, false);
            fbBg = panelGo.GetComponent<Image>();
            fbBg.color = new Color(0.18f, 0.65f, 0.29f);
            SetAnchors(panelGo.GetComponent<RectTransform>(), 0f,0f, 1f,1f);
            fbPanel = panelGo;
            fbPanel.SetActive(false);

            MakeImage(panelGo.transform, "TopAccent",
                new Color(0f,0f,0f,0.18f), Anchors(0f,0.88f,1f,1f));

            fbHeader = MakeTMP(panelGo.transform, "Header", "¡Correcto!",
                30f, Color.white, TextAlignmentOptions.Center);
            fbHeader.fontStyle = FontStyles.Bold;
            SetAnchors(fbHeader.rectTransform, 0.05f,0.72f, 0.95f,0.94f);

            fbDetail = MakeTMP(panelGo.transform, "Detail", "",
                15f, Color.white, TextAlignmentOptions.TopLeft);
            fbDetail.enableWordWrapping = true;
            SetAnchors(fbDetail.rectTransform, 0.06f,0.40f, 0.94f,0.72f);

            fbIndicators = MakeTMP(panelGo.transform, "Indicators", "",
                13f, new Color(1f,1f,0.72f), TextAlignmentOptions.TopLeft);
            fbIndicators.enableWordWrapping = true;
            SetAnchors(fbIndicators.rectTransform, 0.06f,0.05f, 0.94f,0.40f);
        }

        // ════════════════════════════════════════════════════════════════════
        //  Cableado
        // ════════════════════════════════════════════════════════════════════

        static void WireAll(
            GameObject browserGo, GameObject feedbackGo,
            TMP_Text urlTMP, GameObject padValid, GameObject padInvalid,
            TMP_Text tabTitleTMP, TMP_Text logoTMP, Image pageHeaderBg,
            TMP_Text subtitleTMP, Image loginBtnImg, TMP_Text loginBtnLbl,
            TMP_Text formTMP, RectTransform formCont, GameObject excessWarn,
            Button btnClose, Button btnEnter,
            GameObject fbPanel, TMP_Text fbHeader, TMP_Text fbDetail,
            TMP_Text fbIndicators, Image fbBg)
        {
            // BrowserController
            var bc = FindInScene<BrowserController>();
            if (bc == null)
            {
                var g = new GameObject("E2_BrowserController");
                Undo.RegisterCreatedObjectUndo(g, "Crear BrowserController");
                bc = g.AddComponent<BrowserController>();
            }
            var bcSo = new SerializedObject(bc);
            SetProp(bcSo, "urlText",                urlTMP);
            SetProp(bcSo, "padlockValid",           padValid);
            SetProp(bcSo, "padlockInvalid",         padInvalid);
            SetProp(bcSo, "browserTabTitle",        tabTitleTMP);
            SetProp(bcSo, "logoText",               logoTMP);
            SetProp(bcSo, "pageHeaderBackground",   pageHeaderBg);
            SetProp(bcSo, "pageSubtitle",           subtitleTMP);
            SetProp(bcSo, "loginButtonImage",       loginBtnImg);
            SetProp(bcSo, "loginButtonLabel",       loginBtnLbl);
            SetProp(bcSo, "formFieldsText",         formTMP);
            SetProp(bcSo, "formFieldsContainer",    formCont);
            SetProp(bcSo, "excessiveFieldsWarning", excessWarn);
            bcSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(bc);

            // E2FeedbackPanel
            var fp = feedbackGo.GetComponent<E2FeedbackPanel>()
                     ?? feedbackGo.AddComponent<E2FeedbackPanel>();
            var fpSo = new SerializedObject(fp);
            SetProp(fpSo, "panel",           fbPanel);
            SetProp(fpSo, "headerText",      fbHeader);
            SetProp(fpSo, "detailText",      fbDetail);
            SetProp(fpSo, "indicatorsText",  fbIndicators);
            SetProp(fpSo, "panelBackground", fbBg);
            fpSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(fp);

            // E2Manager
            var mgr = FindInScene<E2Manager>();
            if (mgr == null)
            {
                var g = new GameObject("E2Manager");
                Undo.RegisterCreatedObjectUndo(g, "Crear E2Manager");
                mgr = g.AddComponent<E2Manager>();
            }
            var mgrSo = new SerializedObject(mgr);
            SetProp(mgrSo, "browser",             bc);
            SetProp(mgrSo, "feedbackPanel",       fp);
            SetProp(mgrSo, "btnReportSite",       btnClose);
            SetProp(mgrSo, "btnEnterCredentials", btnEnter);

            string[] sitePaths =
            {
                $"{SiteDataFolder}/Site_BancoEstadoFalso.asset",
                $"{SiteDataFolder}/Site_RedSocialFalsa.asset",
                $"{SiteDataFolder}/Site_BancoEstadoReal.asset",
            };
            var sitesProp = mgrSo.FindProperty("sites");
            sitesProp.arraySize = sitePaths.Length;
            for (int i = 0; i < sitePaths.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<SiteData>(sitePaths[i]);
                if (asset == null)
                    Debug.LogWarning($"[E2Builder] No encontrado: {sitePaths[i]} " +
                        "— corre primero 'Tools → PhishingVR → Generar SiteData E2'.");
                sitesProp.GetArrayElementAtIndex(i).objectReferenceValue = asset;
            }
            mgrSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(mgr);
        }

        // ════════════════════════════════════════════════════════════════════
        //  Posicionamiento en la escena Demo
        // ════════════════════════════════════════════════════════════════════

        static void AnchorToSelectedMonitor(GameObject canvasGo)
        {
            var monitor = Selection.activeTransform;
            if (monitor == null)
            {
                if (canvasGo.transform.position == Vector3.zero)
                    canvasGo.transform.SetPositionAndRotation(
                        new Vector3(0f, 1.2f, 1.5f), Quaternion.identity);
                Debug.Log("[E2Builder] Sin monitor seleccionado — canvas en (0,1.2,1.5).");
                return;
            }
            Renderer rend = monitor.GetComponentInChildren<Renderer>();
            if (rend == null) { Debug.LogWarning("[E2Builder] Sin Renderer en el objeto seleccionado."); return; }

            Bounds b = rend.bounds;
            Camera cam = GetXrCamera();
            Vector3 toUser = cam != null
                ? (cam.transform.position - b.center)
                : monitor.forward;
            toUser.y = 0f;
            if (toUser.sqrMagnitude < 1e-4f) toUser = monitor.forward;
            toUser.Normalize();

            Undo.RecordObject(canvasGo.transform, "Anclar canvas al monitor");
            float offset = b.extents.magnitude * 0.5f + 0.015f;
            canvasGo.transform.SetPositionAndRotation(
                b.center + toUser * offset,
                Quaternion.LookRotation(-toUser, Vector3.up));
            canvasGo.transform.SetParent(monitor, worldPositionStays: true);
            Debug.Log($"[E2Builder] Canvas anclado a '{monitor.name}'.");
        }

        static void PlaceFeedback(GameObject canvasGo)
        {
            Camera cam = GetXrCamera();
            if (cam == null || canvasGo.transform.position != Vector3.zero) return;
            Vector3 fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
            canvasGo.transform.SetPositionAndRotation(
                cam.transform.position + fwd * 1.5f,
                Quaternion.LookRotation(fwd));
        }

        static Camera GetXrCamera()
        {
            var xr = Object.FindFirstObjectByType<XROrigin>();
            return xr != null && xr.Camera != null ? xr.Camera : Camera.main;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Helpers de UI
        // ════════════════════════════════════════════════════════════════════

        static GameObject GetOrCreateCanvas(string name, Vector2 size, float scale)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                Undo.RegisterCreatedObjectUndo(go, $"Crear {name}");
            }
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            go.GetComponent<RectTransform>().sizeDelta = size;
            if (go.transform.localScale == Vector3.one)
                go.transform.localScale = Vector3.one * scale;
            EnsureRaycaster(go);
            return go;
        }

        static void ClearChildren(GameObject p)
        {
            var list = new List<GameObject>();
            foreach (Transform c in p.transform) list.Add(c.gameObject);
            foreach (var c in list) Undo.DestroyObjectImmediate(c);
        }

        // Crea un GO con Image y opcionalmente aplica anchors
        static Image MakeImage(Transform parent, string name, Color color,
                                System.Action<RectTransform> layout)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "E2Build");
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            layout?.Invoke(go.GetComponent<RectTransform>());
            return img;
        }

        // Devuelve action que aplica anchors full-stretch
        static System.Action<RectTransform> Anchors(float x0, float y0, float x1, float y1)
            => rt => SetAnchors(rt, x0, y0, x1, y1);

        static void SetAnchors(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static TMP_Text MakeTMP(Transform parent, string name, string text,
                                  float size, Color color,
                                  TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "E2Build");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false;
            return t;
        }

        static Button MakeButton(Transform parent, string name, string label,
                                  Color bgColor, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "E2Build");
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = bgColor;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var lblGo = new GameObject("Label", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(lblGo, "E2Build");
            lblGo.transform.SetParent(go.transform, false);
            var lbl = lblGo.AddComponent<TextMeshProUGUI>();
            lbl.text = label; lbl.fontSize = fontSize;
            lbl.fontStyle = FontStyles.Bold;
            lbl.color = Color.white;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.raycastTarget = false;
            SetAnchors(lblGo.GetComponent<RectTransform>(), 0f,0f, 1f,1f);
            return btn;
        }

        static void EnsureRaycaster(GameObject go)
        {
            if (go.GetComponent<TrackedDeviceGraphicRaycaster>() != null) return;
            var std = go.GetComponent<GraphicRaycaster>();
            if (std != null) Object.DestroyImmediate(std);
            go.AddComponent<TrackedDeviceGraphicRaycaster>();
        }

        static T FindInScene<T>() where T : Component
        {
            foreach (var c in Resources.FindObjectsOfTypeAll<T>())
            {
                if (EditorUtility.IsPersistent(c.gameObject)) continue;
                if (!c.gameObject.scene.IsValid()) continue;
                return c;
            }
            return null;
        }

        static void SetProp(SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.objectReferenceValue = value;
            else Debug.LogWarning($"[E2Builder] '{prop}' no encontrado en {so.targetObject.GetType().Name}.");
        }

        static void MarkDirty() =>
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
#endif
