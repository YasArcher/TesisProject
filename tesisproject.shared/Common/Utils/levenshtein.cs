using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace tesisproject.shared.Common.Utils
{
    /// <summary>
    /// Utilities for string distance and similarity using Levenshtein.
    /// Optimized memory usage: O(min(n, m)) space, O(n*m) time.
    /// </summary>
    public static class Levenshtein
    {
        /// <summary>
        /// Returns the Levenshtein distance between two strings using a memory-optimized DP
        /// (keeps only the current and previous rows).
        /// </summary>
        public static int LevenshteinDistance(string? a, string? b, bool normalize = true)
        {
            if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
            if (string.IsNullOrEmpty(b)) return a.Length;

            if (normalize)
            {
                a = NormalizeForComparison(a);
                b = NormalizeForComparison(b);
            }

            // Ensure 'a' is the shorter string to reduce memory (O(min(n,m))).
            if (a.Length > b.Length)
            {
                var tmpSwap = a; a = b; b = tmpSwap;
            }

            int n = a.Length;
            int m = b.Length;

            // prev[j] = distance(a[0..i-1], b[0..j])
            // curr[j] = distance(a[0..i],   b[0..j])
            int[] prev = new int[m + 1];
            int[] curr = new int[m + 1];

            for (int j = 0; j <= m; j++)
                prev[j] = j;

            for (int i = 1; i <= n; i++)
            {
                curr[0] = i;

                char ca = a[i - 1];
                for (int j = 1; j <= m; j++)
                {
                    int cost = (ca == b[j - 1]) ? 0 : 1;

                    int deletion = prev[j] + 1;
                    int insertion = curr[j - 1] + 1;
                    int substitution = prev[j - 1] + cost;

                    curr[j] = Math.Min(Math.Min(deletion, insertion), substitution);
                }

                // Swap row buffers to avoid reallocations
                var tmp = prev; prev = curr; curr = tmp;
            }

            return prev[m];
        }

        /// <summary>
        /// Returns a similarity percentage in [0..100], where 100 means identical strings.
        /// Based on Levenshtein distance normalized by the max length.
        /// </summary>
        public static double SimilarityPercentage(string? a, string? b, bool normalize = true)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 100.0;
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            {
                int maxLen = Math.Max(a?.Length ?? 0, b?.Length ?? 0);
                return maxLen == 0 ? 100.0 : 0.0;
            }

            // Normalize before computing maxLen for fairness
            string aN = normalize ? NormalizeForComparison(a!) : a!;
            string bN = normalize ? NormalizeForComparison(b!) : b!;

            int maxLength = Math.Max(aN.Length, bN.Length);
            if (maxLength == 0) return 100.0;

            int distance = LevenshteinDistance(aN, bN, normalize: false);
            double similarity = 1.0 - (double)distance / maxLength;
            return Math.Max(0.0, Math.Min(1.0, similarity)) * 100.0;
        }

        /// <summary>
        /// Normalizes a string for comparison: trims, lowercases (invariant), removes diacritics, collapses spaces.
        /// </summary>
        public static string NormalizeForComparison(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            // Trim + lowercase invariant
            s = s.Trim().ToLowerInvariant();

            // Remove diacritics (accents)
            string formD = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(capacity: formD.Length);
            foreach (var ch in formD)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            s = sb.ToString().Normalize(NormalizationForm.FormC);

            // Collapse multiple spaces to single space
            s = Regex.Replace(s, @"\s+", " ").Trim();

            return s;
        }
    }
}
