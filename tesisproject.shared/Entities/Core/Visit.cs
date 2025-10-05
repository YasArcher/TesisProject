using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Catalogs;

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

        public int? DocumentId { get; set; }             // id_documento (FK -> Document, opcional)

        [Required]
        public int PerformedByUserId { get; set; }       // id_usuario_responsable (quien realiza la visita)

        // ================================
        //        Core Information
        // ================================
        public DateTime? ScheduledDate { get; set; }     // fecha_programada
        public DateTime? PerformedDate { get; set; }     // fecha_realizacion

        [StringLength(500)]
        public string? Notes { get; set; }               // observaciones

        // ================================
        //            Audit
        // ================================
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // fecha_registro

        // ================================
        //      Navigation Properties
        // ================================
        public Project Project { get; set; } = null!;            // Navegación a Project
        public VisitState VisitState { get; set; } = null!;      // Navegación a VisitState (catálogo)
        public Document? Document { get; set; }                  // Navegación a Document (opcional)
        // public UserSystem PerformedByUser { get; set; } = null!; // (si la entidad existe)
    }
}
