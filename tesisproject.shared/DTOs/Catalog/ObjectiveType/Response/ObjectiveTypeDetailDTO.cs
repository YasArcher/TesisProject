using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Catalog.ObjectiveType.Response
{
    /// <summary>
    /// Detailed DTO for single item retrieval.
    /// (Same fields as list for now; ready to extend later if needed.)
    /// </summary>
    public class ObjectiveTypeDetailDTO
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}