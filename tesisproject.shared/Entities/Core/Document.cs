using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.Entities.Core
{
    public class Document
    {
        public Guid DocumentId { get; set; }               // PK
        public Guid? RelatedDocumentId { get; set; }       // FK al único documento relacionado

        [Required]
        public Guid DocumentTypeId { get; set; }              // FK -> DocumentType (catálogo)

        [Required, StringLength(300)]
        public string DocumentPath { get; set; } = string.Empty;

        // Navegaciones
        public Document? RelatedDocument { get; set; }     // único “hijo” o “relacionado”
        public Document? ReverseRelation { get; set; }     // si quieres navegar de vuelta
        public DocumentType DocumentType { get; set; } = null!;
    }

}
