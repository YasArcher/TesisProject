using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.Entities.Core
{
    public class Project
    {
        public int ProjectId { get; set; }        // id_proyecto (string en tu diagrama)
        [StringLength(20)]
        public string ProjectCode { get; set; } = string.Empty; // código_proyecto (código único generado)

        [Required]
        public int CreatedByUserId { get; set; }                 // id_creado_por (usuario externo)
        [Required]
        public int ProjectTypeId { get; set; }                   // id_tipo_proyecto (catálogo)
        [Required]
        public int ProjectStateId { get; set; }              // id_tipo_estado_proyecto (catálogo)
        [Required]
        public int ProjectGroupId { get; set; }                  // id_grupo_proyecto (FK -> Group)
        public int? SenesytGroupId { get; set; }                // id_grupo_senseyt (si aplica, nullable)
        public int FundingTypeId { get; set; }               // id_tipo_financiamiento (catálogo)
        public int? InitialDocumentId { get; set; }              // id_documento_inicial (FK -> Document)
        [Required, StringLength(120)]
        public string ProjectName { get; set; } = string.Empty;  // nombre_proyecto

        [StringLength(500)]
        public string? ProjectObjective { get; set; }            // objetivo_proyecto

        [StringLength(200)]
        public int ResearchLineTypeId { get; set; }                // linea_investigacion_proyecto
        public int KnowledgeAreaTypeId { get; set; }             // area_conocimiento_proyecto (catálogo)
        public string ApprovalResolution { get; set; } = string.Empty; // resolucion_aprobacion
        [Column(TypeName = "date")]
        public DateTime ApprovalDate { get; set; } // fecha_aprobacion

        [Column(TypeName = "date")]
        public DateTime? StartDate { get; set; }                 // fecha_inicio
        public int DurationInMonths { get; set; }              // duracion_meses

        [Column(TypeName = "date")]
        public DateTime? TentativeEndDate { get; set; }          // fecha_fin_tentativa

        [Column(TypeName = "date")]
        public DateTime? RealEndDate { get; set; }               // fecha_fin_real

        [Range(0, 100)]
        public decimal? ExecutionPercentage { get; set; }        // porcentaje_de_ejecucion
        //Navegaciones
        public ProjectType ProjectType { get; set; } = null!;   // Navegación a ProjectType (catálogo)
        public ProjectState ProjectState { get; set; } = null!; // Navegación a ProjectState (catálogo)
        public Group ProjectGroup { get; set; } = null!;       // Navegación a Group
        public Group? SenesytGroup { get; set; }               // Navegación a Group (si aplica)
        public Document? InitialDocument { get; set; }         // Navegación a Document (si aplica)
        public Budget? Budget { get; set; }                         // Navegación a Budget (1 a 1)
        public FundingType FundingType { get; set; } = null!; // Navegación a FundingType (catálogo)
        public ResearchLineType ResearchLineType { get; set; } = null!; // Navegación a ResearchLineType (catálogo)
        public KnowledgeAreaType KnowledgeAreaType { get; set; } = null!; // Navegación a KnowledgeAreaType (catálogo)
    }
}
