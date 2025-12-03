using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Common.Utils
{
    /// <summary>
    /// Utilities for string similarity using the Jaro–Winkler metric.
    /// Jaro–Winkler is especially useful for person names and short strings.
    /// </summary>
    public static class JaroWinkler
    {
        /// <summary>
        /// Returns the Jaro–Winkler similarity in [0..1],
        /// where 1 means identical strings.
        /// </summary>
        /// <param name="a">First string to compare.</param>
        /// <param name="b">Second string to compare.</param>
        /// <param name="normalize">
        /// If true, applies the same normalization as Levenshtein.NormalizeForComparison
        /// (trim, lowercase invariant, remove diacritics, collapse spaces).
        /// </param>
        /// <param name="prefixScale">
        /// Winkler prefix scale factor (typical value = 0.1).
        /// Set to 0 to disable the Winkler boost and get pure Jaro.
        /// </param>
        /// <param name="maxPrefixLength">
        /// Maximum length of common prefix considered for the Winkler boost (typical = 4).
        /// </param>
        /// <param name="boostThreshold">
        /// Minimum Jaro similarity required to apply the Winkler boost (typical = 0.7).
        /// </param>
        public static double Similarity(
            string? a,
            string? b,
            bool normalize = true,
            double prefixScale = 0.1,
            int maxPrefixLength = 4,
            double boostThreshold = 0.7)
        {
            // Both null/empty
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b))
                return 1.0;

            // One null/empty, la otra no
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return 0.0;

            if (normalize)
            {
                a = Levenshtein.NormalizeForComparison(a!);
                b = Levenshtein.NormalizeForComparison(b!);
            }

            // Si después de normalizar quedaron vacías
            if (a!.Length == 0 && b!.Length == 0)
                return 1.0;

            if (a.Length == 0 || b.Length == 0)
                return 0.0;

            // Jaro base
            double jaro = ComputeJaro(a, b);
            double jw = jaro;

            // Winkler boost
            if (prefixScale > 0 && jaro > boostThreshold)
            {
                int prefixLength = 0;
                int maxPrefix = Math.Min(Math.Min(maxPrefixLength, a.Length), b.Length);

                for (int i = 0; i < maxPrefix; i++)
                {
                    if (a[i] == b[i])
                        prefixLength++;
                    else
                        break;
                }

                if (prefixLength > 0)
                {
                    jw = jaro + prefixLength * prefixScale * (1.0 - jaro);
                }
            }

            // Clamp por seguridad numérica
            if (jw < 0) jw = 0;
            if (jw > 1) jw = 1;

            return jw;
        }

        /// <summary>
        /// Returns the Jaro–Winkler similarity as a percentage in [0..100].
        /// </summary>
        public static double SimilarityPercentage(
            string? a,
            string? b,
            bool normalize = true,
            double prefixScale = 0.1,
            int maxPrefixLength = 4,
            double boostThreshold = 0.7)
        {
            var sim = Similarity(a, b, normalize, prefixScale, maxPrefixLength, boostThreshold);
            return sim * 100.0;
        }

        /// <summary>
        /// Computes the base Jaro similarity (without Winkler boost) in [0..1].
        /// </summary>
        private static double ComputeJaro(string s1, string s2)
        {
            int len1 = s1.Length;
            int len2 = s2.Length;

            if (len1 == 0 && len2 == 0) return 1.0;
            if (len1 == 0 || len2 == 0) return 0.0;

            int matchDistance = Math.Max(len1, len2) / 2 - 1;
            if (matchDistance < 0) matchDistance = 0;

            bool[] s1Matches = new bool[len1];
            bool[] s2Matches = new bool[len2];

            int matches = 0;
            int transpositions = 0;

            // Buscar caracteres coincidentes dentro de la ventana de matchDistance
            for (int i = 0; i < len1; i++)
            {
                int start = Math.Max(0, i - matchDistance);
                int end = Math.Min(i + matchDistance + 1, len2);

                for (int j = start; j < end; j++)
                {
                    if (s2Matches[j]) continue;
                    if (s1[i] != s2[j]) continue;

                    s1Matches[i] = true;
                    s2Matches[j] = true;
                    matches++;
                    break;
                }
            }

            if (matches == 0)
                return 0.0;

            // Contar transposiciones
            int k = 0;
            for (int i = 0; i < len1; i++)
            {
                if (!s1Matches[i]) continue;

                while (!s2Matches[k])
                    k++;

                if (s1[i] != s2[k])
                    transpositions++;

                k++;
            }

            double m = matches;
            double t = transpositions / 2.0;

            // Fórmula Jaro
            double jaro =
                (m / len1 +
                 m / len2 +
                 (m - t) / m) / 3.0;

            return jaro;
        }
    }
}