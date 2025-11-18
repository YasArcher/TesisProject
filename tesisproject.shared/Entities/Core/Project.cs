using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Entities.Core.Products;

namespace tesisproject.shared.Entities.Core
{
    public class Project
    {
        // ================================
        //            Keys / Codes
        // ================================
        public int ProjectId { get; set; }        // id_proyecto (string en tu diagrama)

        [StringLength(20)]
        public string ProjectCode { get; set; } = string.Empty; // código_proyecto (código único generado)

        // ================================
        //            Foreign Keys
        // ================================
        [Required]
        public int CreatedByUserId { get; set; }                 // id_creado_por (usuario externo)

        [Required]
        public int ProjectTypeId { get; set; }                   // id_tipo_proyecto (catálogo)

        [Required]
        public int ProjectStateId { get; set; }                  // id_tipo_estado_proyecto (catálogo)

        [Required]
        public int ProjectGroupId { get; set; }                  // id_grupo_proyecto (FK -> Group)

        public int FundingTypeId { get; set; }                   // id_tipo_financiamiento (catálogo)

        public int? InitialDocumentId { get; set; }              // id_documento_inicial (FK -> Document)

        // ================================
        //          Core Information
        // ================================
        [Required, StringLength(120)]
        public string ProjectName { get; set; } = string.Empty;  // nombre_proyecto

        [StringLength(200)]
        public int ResearchLineTypeId { get; set; }              // linea_investigacion_proyecto

        // ================================
        //              Dates
        // ================================
        [Column(TypeName = "date")]
        public DateTime ApprovalDate { get; set; }               // fecha_aprobacion

        [Column(TypeName = "date")]
        public DateTime? StartDate { get; set; }                 // fecha_inicio

        public int DurationInMonths { get; set; }                // duracion_meses

        [Column(TypeName = "date")]
        public DateTime? TentativeEndDate { get; set; }          // fecha_fin_tentativa

        [Column(TypeName = "date")]
        public DateTime? RealEndDate { get; set; }               // fecha_fin_real

        // ================================
        //             Metrics
        // ================================
        [Range(0, 100)]
        public decimal? ExecutionPercentage { get; set; }        // porcentaje_de_ejecucion
        [Required]
        public int FacultyId { get; set; }

        // ================================
        //        Navigation Properties
        // ================================
        public ProjectType ProjectType { get; set; } = null!;            // Navegación a ProjectType (catálogo)
        public ProjectState ProjectState { get; set; } = null!;          // Navegación a ProjectState (catálogo)
        public Group ProjectGroup { get; set; } = null!;                 // Navegación a Group
        public Document? InitialDocument { get; set; }                   // Navegación a Document (si aplica)
        public Budget? Budget { get; set; }                               // Navegación a Budget (1 a 1)
        public FundingType FundingType { get; set; } = null!;            // Navegación a FundingType (catálogo)
        public ICollection<ProjectObjective>  ProjectObjectives { get; set; } = null!; // Navegación a ProjectObjective (1 a muchos)


        // ================================
        //      Collections / Many-to-Many
        // ================================
        public ICollection<ExternalResearcherProject> ExternalResearcherProjects { get; set; } = new List<ExternalResearcherProject>();
        public ICollection<Visit> Visits { get; set; } = new List<Visit>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<ProjectResearchLine> ProjectResearchLines { get; set; } = new List<ProjectResearchLine>();
    }
}
