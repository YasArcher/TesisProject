using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Common.Utils
{
    public static class NameSimilarity
    {
        /// <summary>
        /// Computes a similarity percentage [0..100] for full names with
        /// arbitrary number of tokens (first, middle, last names, etc.).
        /// Uses token-level Jaro–Winkler matching.
        /// </summary>
        public static double FlexibleFullNameSimilarityPercentage(
            string? fullNameA,
            string? fullNameB,
            bool normalize = true)
        {
            if (string.IsNullOrWhiteSpace(fullNameA) && string.IsNullOrWhiteSpace(fullNameB))
                return 100.0;

            if (string.IsNullOrWhiteSpace(fullNameA) || string.IsNullOrWhiteSpace(fullNameB))
                return 0.0;

            if (normalize)
            {
                fullNameA = Levenshtein.NormalizeForComparison(fullNameA!);
                fullNameB = Levenshtein.NormalizeForComparison(fullNameB!);
            }

            var partsA = fullNameA!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var partsB = fullNameB!.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (partsA.Length == 0 && partsB.Length == 0) return 100.0;
            if (partsA.Length == 0 || partsB.Length == 0) return 0.0;

            // Promedio de mejores coincidencias A -> B
            double avgAB = AverageBestTokenSimilarity(partsA, partsB);

            // Promedio de mejores coincidencias B -> A
            double avgBA = AverageBestTokenSimilarity(partsB, partsA);

            // Simetrizamos
            double combined = (avgAB + avgBA) / 2.0;

            return combined * 100.0;
        }

        /// <summary>
        /// For each token in source, finds the best Jaro–Winkler match in target
        /// and returns the average similarity [0..1].
        /// </summary>
        private static double AverageBestTokenSimilarity(string[] source, string[] target)
        {
            if (source.Length == 0 || target.Length == 0)
                return 0.0;

            double sum = 0.0;

            for (int i = 0; i < source.Length; i++)
            {
                double best = 0.0;
                string s = source[i];

                for (int j = 0; j < target.Length; j++)
                {
                    string t = target[j];
                    double sim = JaroWinkler.Similarity(s, t, normalize: false);

                    if (sim > best)
                        best = sim;
                }

                // Podrías dar un peso extra a primer/último token aquí si quisieras.
                sum += best;
            }

            return sum / source.Length;
        }
    }
}
