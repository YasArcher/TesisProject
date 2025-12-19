using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.Entities.Core
{
    public class Document
    {
        [Key]
        public int DocumentId { get; set; }

        // ================================
        //          Type / Storage
        // ================================
        [Required]
        public int DocumentTypeId { get; set; } // FK -> DocumentType

        [Required, StringLength(300)]
        public string DocumentPath { get; set; } = string.Empty;

        // ================================
        //      Resolution (optional)
        // ================================
        [StringLength(100)]
        public string? ResolutionCode { get; set; }

        public DateTime? ResolutionDate { get; set; }

        // ================================
        //               Audit
        // ================================
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int CreatedByUserId { get; set; }
        public int? UpdatedByUserId { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // ================================
        //           Navigations
        // ================================
        [ForeignKey(nameof(DocumentTypeId))]
        public DocumentType DocumentType { get; set; } = null!;

        public ICollection<ProjectDocument> ProjectDocuments { get; set; } = new List<ProjectDocument>();
    }
}
