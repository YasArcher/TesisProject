using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Document.Request
{
    public class UploadDocumentClientDTO
    {
        public int DocumentTypeId { get; set; }
        public int? RelatedDocumentId { get; set; }
        public string? ResolutionCode { get; set; }
        public DateTime? ResolutionDate { get; set; }
    }
}
