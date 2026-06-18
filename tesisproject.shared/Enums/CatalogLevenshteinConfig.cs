using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Enums
{
    /// <summary>
    /// Levenshtein guardrails for catalog naming.
    /// Values are stored as ints to be enum-friendly.
    /// </summary>
    public enum CatalogLevenshteinConfig
    {
        /// <summary>
        /// Similarity threshold in percentage (0-100).
        /// </summary>
        SimilarityThresholdPercent = 90,

        /// <summary>
        /// Maximum number of candidate suggestions to show.
        /// </summary>
        MaxCandidates = 5
    }
}