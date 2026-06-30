// Este script solo existe en el Editor de Unity — NO se incluye en la build final.
// Uso: menú superior → Tools → PhishingVR → Generar SiteData E2

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using PhishingVR.E2;

namespace PhishingVR.E2.Editor
{
    public static class E2SiteDataGenerator
    {
        private const string OutputFolder = "Assets/Data/E2";

        [MenuItem("Tools/PhishingVR/Generar SiteData E2")]
        public static void GenerateAll()
        {
            // Crear carpeta si no existe
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "E2");
                Debug.Log($"[E2SiteDataGenerator] Carpeta creada: {OutputFolder}");
            }

            CreateAsset(MakeBancoFalso(),       "Site_BancoEstadoFalso");
            CreateAsset(MakeRedSocialFalsa(),    "Site_RedSocialFalsa");
            CreateAsset(MakeBancoReal(),         "Site_BancoEstadoReal");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[E2SiteDataGenerator] ✅ 3 SiteData assets creados en Assets/Data/E2/");
            EditorUtility.DisplayDialog(
                "PhishingVR — SiteData generados",
                "Se crearon 3 assets en Assets/Data/E2/:\n\n" +
                "• Site_BancoEstadoFalso  (phishing)\n" +
                "• Site_RedSocialFalsa    (phishing)\n" +
                "• Site_BancoEstadoReal   (legítimo)\n\n" +
                "Arrástralos al campo 'Sites' del E2Manager en el orden que quieras.",
                "OK");
        }

        // ════════════════════════════════════════════════════════════════════
        //  Definición de los sitios
        // ════════════════════════════════════════════════════════════════════

        private static SiteData MakeBancoFalso()
        {
            var s = ScriptableObject.CreateInstance<SiteData>();
            s.SiteId       = "site_banco_falso";
            s.DisplayName  = "BancoEstado";
            s.DisplayURL   = "http://bancestado-cl.tk/login";          // sin HTTPS, dominio raro
            s.HasHTTPS     = false;
            s.LogoText     = "BancEstado";                              // typo intencional
            s.IsPhishing   = true;
            s.ActiveIndicators =
                PhishingIndicator.BadURL |
                PhishingIndicator.NoHTTPS |
                PhishingIndicator.TypoInLogo |
                PhishingIndicator.ExcessiveFormFields;
            s.FormFieldLabels  = new[] { "RUT", "Clave", "Clave Tarjeta", "PIN", "Fecha Nacimiento" };
            s.EducationalExplanation =
                "Este sitio tenía varias señales de alerta:\n" +
                "• La URL usaba HTTP (sin candado), no HTTPS.\n" +
                "• El dominio 'bancestado-cl.tk' no es el oficial 'bancoestado.cl'.\n" +
                "• El logotipo tenía un error tipográfico ('BancEstado').\n" +
                "• Se pedían datos excesivos como PIN y fecha de nacimiento.";
            return s;
        }

        private static SiteData MakeRedSocialFalsa()
        {
            var s = ScriptableObject.CreateInstance<SiteData>();
            s.SiteId       = "site_red_social_falsa";
            s.DisplayName  = "Facebook";
            s.DisplayURL   = "https://faceb00k-login.com/verify";      // dominio falso con 0s
            s.HasHTTPS     = true;                                      // HTTPS no garantiza legitimidad
            s.LogoText     = "facebook";
            s.IsPhishing   = true;
            s.ActiveIndicators =
                PhishingIndicator.BadURL |
                PhishingIndicator.ExcessiveFormFields;
            s.FormFieldLabels = new[] { "Email", "Contraseña", "Número de teléfono", "Código SMS" };
            s.EducationalExplanation =
                "Este sitio tenía señales de alerta aunque usaba HTTPS:\n" +
                "• El dominio era 'faceb00k-login.com' (con ceros en lugar de 'o').\n" +
                "• HTTPS solo garantiza que la conexión está cifrada, no que el sitio sea legítimo.\n" +
                "• Facebook nunca pide tu código SMS en el login normal.";
            return s;
        }

        private static SiteData MakeBancoReal()
        {
            var s = ScriptableObject.CreateInstance<SiteData>();
            s.SiteId       = "site_banco_real";
            s.DisplayName  = "BancoEstado (legítimo)";
            s.DisplayURL   = "https://www.bancoestado.cl/imagenes/Home/login.asp";
            s.HasHTTPS     = true;
            s.LogoText     = "BancoEstado";
            s.IsPhishing   = false;
            s.ActiveIndicators = PhishingIndicator.None;
            s.FormFieldLabels  = new[] { "RUT", "Clave" };
            s.EducationalExplanation = string.Empty;  // no aplica para sitio legítimo
            return s;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Helpers
        // ════════════════════════════════════════════════════════════════════

        private static void CreateAsset(SiteData asset, string fileName)
        {
            string path = $"{OutputFolder}/{fileName}.asset";

            // Si ya existe, no sobreescribir (para no perder cambios manuales)
            if (File.Exists(Path.Combine(Application.dataPath, $"Data/E2/{fileName}.asset")))
            {
                Debug.LogWarning($"[E2SiteDataGenerator] Ya existe: {path} — se omite.");
                return;
            }

            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[E2SiteDataGenerator] Creado: {path}");
        }
    }
}
#endif
