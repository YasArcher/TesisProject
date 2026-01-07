using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Document.Request
{
    public class UpdateDocumentRequestDTO
    {
        [Range(1, int.MaxValue)]
        public int DocumentTypeId { get; set; }

        [StringLength(100)]
        public string? ResolutionCode { get; set; }

        public DateTime? ResolutionDate { get; set; }
    }
}