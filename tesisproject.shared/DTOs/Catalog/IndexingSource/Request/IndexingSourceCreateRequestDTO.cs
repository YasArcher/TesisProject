using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Catalog.IndexingSource.Request
{
    public class IndexingSourceCreateRequestDTO
    {
        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional abbreviation (e.g., "WoS", "SCOPUS").
        /// </summary>
        public string? Abbreviation { get; set; }

        /// <summary>
        /// Optional URL or reference page for the index.
        /// </summary>
        public string? ReferenceUrl { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;
    }
}