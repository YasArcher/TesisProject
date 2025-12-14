using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.Entities.Core
{
    public class Document
    {
        // ================================
        //              Identity
        // ================================
        [Key]
        public int DocumentId { get; set; }

        // Autorreferencia (1:1). Se refuerza unicidad por Fluent API.
        public int? RelatedDocumentId { get; set; }

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
        public string? ResolutionCode { get; set; } // Número/código de resolución

        public DateTime? ResolutionDate { get; set; } // Fecha de resolución

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

        // Documento “pareja” (relación 1:1)
        [ForeignKey(nameof(RelatedDocumentId))]
        [InverseProperty(nameof(ReverseRelation))]
        public Document? RelatedDocument { get; set; }

        // Navegación inversa (1:1)
        [InverseProperty(nameof(RelatedDocument))]
        public Document? ReverseRelation { get; set; }

        // 🔹 Nueva navegación: proyectos que referencian este documento
        public ICollection<ProjectDocument> ProjectDocuments { get; set; } = new List<ProjectDocument>();
    }
}