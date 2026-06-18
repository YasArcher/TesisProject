using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Catalog.IndexingSource.Request
{
    public class IndexingSourceUpdateRequestDTO
    {
        [Required]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Abbreviation { get; set; }

        public string? ReferenceUrl { get; set; }

        [Required]
        public bool IsActive { get; set; }
    }
}