using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace PhishingVR.Research
{
    /// <summary>
    /// Escritor de CSV reutilizable para la telemetría del paper
    /// (Experimento A — frametime, Experimento B — latencia de mitigación).
    ///
    /// - Crea el archivo en <c>Application.persistentDataPath/benchmarks/</c>
    ///   con nombre <c>{prefijo}_{yyyyMMdd_HHmmss}.csv</c>.
    /// - Fuerza <see cref="CultureInfo.InvariantCulture"/> en TODOS los números:
    ///   imprescindible porque la máquina está en es-CL y el separador decimal
    ///   por defecto sería coma — eso rompería el CSV (la coma es el separador de
    ///   columnas). Aquí los decimales SIEMPRE usan punto.
    ///
    /// Uso:
    /// <code>
    ///   using var log = new BenchmarkCsvLogger("expA_render", "technique,n,mean_ms");
    ///   log.WriteRow("Baseline", 10, 11.3456f);
    /// </code>
    /// </summary>
    public sealed class BenchmarkCsvLogger : IDisposable
    {
        private readonly StreamWriter _writer;

        /// <summary>Ruta absoluta del CSV en disco (útil para loguearla en consola).</summary>
        public string FilePath { get; }

        public BenchmarkCsvLogger(string fileNamePrefix, string headerLine)
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string dir = Path.Combine(Application.persistentDataPath, "benchmarks");
            Directory.CreateDirectory(dir);

            FilePath = Path.Combine(dir, $"{fileNamePrefix}_{stamp}.csv");
            _writer = new StreamWriter(FilePath, append: false, new UTF8Encoding(false));
            _writer.WriteLine(headerLine);
            _writer.Flush();

            Debug.Log($"[BenchmarkCsvLogger] CSV abierto → {FilePath}");
        }

        /// <summary>
        /// Escribe una fila. Los valores numéricos se formatean en cultura invariante
        /// automáticamente; no formatees los floats tú mismo o saldrán con coma.
        /// </summary>
        public void WriteRow(params object[] values)
        {
            var sb = new StringBuilder(values.Length * 8);
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(Format(values[i]));
            }
            _writer.WriteLine(sb.ToString());
        }

        private static string Format(object v)
        {
            switch (v)
            {
                case null:       return string.Empty;
                case float f:    return f.ToString("R", CultureInfo.InvariantCulture);
                case double d:   return d.ToString("R", CultureInfo.InvariantCulture);
                case bool b:     return b ? "1" : "0";
                case IFormattable formattable:
                                 return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    // Escapado mínimo CSV por si un string trae coma o comilla.
                    string s = v.ToString();
                    if (s.IndexOf(',') >= 0 || s.IndexOf('"') >= 0 || s.IndexOf('\n') >= 0)
                        return "\"" + s.Replace("\"", "\"\"") + "\"";
                    return s;
            }
        }

        public void Flush() => _writer.Flush();

        public void Dispose()
        {
            _writer.Flush();
            _writer.Dispose();
        }
    }
}
