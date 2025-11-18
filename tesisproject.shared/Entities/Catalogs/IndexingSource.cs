using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Base;

namespace tesisproject.shared.Entities.Catalogs
{
    /// <summary>
    /// Catalog of scientific indexing sources (e.g., Scopus, Web of Science, Latindex, SciELO).
    /// Allows dynamic configuration instead of hard-coded enums.
    /// </summary>
    public sealed class IndexingSource : CatalogEntityBase
    {
        /// <summary>
        /// Optional abbreviation (e.g., "WoS", "SCOPUS").
        /// </summary>
        public string? Abbreviation { get; set; }

        /// <summary>
        /// Optional URL or reference page for the index.
        /// </summary>
        public string? ReferenceUrl { get; set; }
    }
}