using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Entities.Core.Products;

namespace tesisproject.shared.Entities.Core
{
    public class Visit
    {
        // ================================
        //              Keys
        // ================================
        public int VisitId { get; set; }                 // id_visita

        // ================================
        //           Foreign Keys
        // ================================
        [Required]
        public int ProjectId { get; set; }               // id_proyecto (FK -> Project)

        [Required]
        public int VisitStateId { get; set; }            // id_estado_visita (FK -> VisitState catálogo)

        [Required]
        public int AcademicPeriodId { get; set; }        // id_periodo_academico (FK -> AcademicPeriod catálogo)

        // 1) Informe económico
        public int? FundingDocumentId { get; set; }      // id_documento_financiamiento (FK -> Document)

        // 2) Informe de visita
        public int? DocumentId { get; set; }             // id_documento_visita (FK -> Document)

        // 3) Informe de avance
        public int? ProgressDocumentId { get; set; }     // id_documento_avance (FK -> Document)

        public int? PerformedByUserId { get; set; }      // id_usuario_responsable (quien realiza la visita)

        // ================================
        //        Core Information
        // ================================
        public DateTime? ScheduledDate { get; set; }     // fecha_programada
        public DateTime? PerformedDate { get; set; }     // fecha_realizacion

        // ================================
        //            Audit
        // ================================
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // fecha_registro

        // ================================
        //      Navigation Properties
        // ================================
        public Project Project { get; set; } = null!;            // Navegación a Project
        public VisitState VisitState { get; set; } = null!;      // Navegación a VisitState (catálogo)
        public Document? Document { get; set; }                  // Informe de visita
        public Document? FundingDocument { get; set; }           // Informe económico
        public Document? ProgressDocument { get; set; }          // Informe de avance
        public AcademicPeriod AcademicPeriod { get; set; } = null!; // Navegación a AcademicPeriod (catálogo)
        public ICollection<VisitIssue> Issues { get; set; } = new List<VisitIssue>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
