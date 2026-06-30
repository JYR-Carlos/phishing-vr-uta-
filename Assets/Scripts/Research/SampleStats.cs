using System.Collections.Generic;

namespace PhishingVR.Research
{
    /// <summary>
    /// Estadística descriptiva de una serie de muestras (frametime o latencias).
    /// Calcula media, mediana, percentiles, min/max y desviación estándar.
    /// Pensada para resumir cada paso de un experimento en una sola fila de CSV.
    /// </summary>
    public struct SampleStats
    {
        public int Count;
        public double Mean;
        public double Median;
        public double P95;
        public double P99;
        public double Min;
        public double Max;
        public double StdDev;

        /// <param name="samples">Se ordena internamente (se modifica la lista recibida).</param>
        public static SampleStats Compute(List<double> samples)
        {
            var stats = new SampleStats { Count = samples?.Count ?? 0 };
            if (stats.Count == 0) return stats;

            samples.Sort();

            double sum = 0d;
            stats.Min = samples[0];
            stats.Max = samples[stats.Count - 1];
            for (int i = 0; i < stats.Count; i++) sum += samples[i];
            stats.Mean = sum / stats.Count;

            double sqSum = 0d;
            for (int i = 0; i < stats.Count; i++)
            {
                double d = samples[i] - stats.Mean;
                sqSum += d * d;
            }
            stats.StdDev = System.Math.Sqrt(sqSum / stats.Count);

            stats.Median = Percentile(samples, 50d);
            stats.P95    = Percentile(samples, 95d);
            stats.P99    = Percentile(samples, 99d);
            return stats;
        }

        // Percentil por interpolación lineal sobre la lista YA ordenada.
        private static double Percentile(List<double> sorted, double p)
        {
            if (sorted.Count == 1) return sorted[0];
            double rank = (p / 100d) * (sorted.Count - 1);
            int lo = (int)rank;
            int hi = System.Math.Min(lo + 1, sorted.Count - 1);
            double frac = rank - lo;
            return sorted[lo] + (sorted[hi] - sorted[lo]) * frac;
        }
    }
}
