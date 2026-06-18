using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.IndexingSource.Request
{
    public class IndexingSourceUpdateRequestDTO
    {
        [Required]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(32)]
        public string? Abbreviation { get; set; }

        [Url, StringLength(256)]
        public string? ReferenceUrl { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
