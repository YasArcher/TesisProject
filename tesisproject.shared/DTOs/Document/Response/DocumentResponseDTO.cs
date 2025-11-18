using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Document.Response
{
    public class DocumentResponseDTO
    {
        public int DocumentId { get; set; }

        public int DocumentTypeId { get; set; }

        public string DocumentPath { get; set; } = string.Empty;

        public string? ResolutionCode { get; set; }

        public DateTime? ResolutionDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public int CreatedByUserId { get; set; }

        public int? RelatedDocumentId { get; set; }
    }
}
