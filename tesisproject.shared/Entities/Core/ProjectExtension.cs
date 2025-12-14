using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.Entities.Core
{
    public class ProjectExtension
    {
        // ================================
        //              Keys
        // ================================
        public int ProjectExtensionId { get; set; }       // id_prorroga

        // ================================
        //           Foreign Keys
        // ================================
        [Required]
        public int ProjectId { get; set; }                // id_proyecto (FK -> Project)

        public int? DocumentId { get; set; }              // id_documento (respaldo, opcional)

        // ================================
        //              Dates
        // ================================
        public DateTime ExtensionDate { get; set; }        // fecha_prorroga
        public DateTime? RequestedAt { get; set; }        // fecha_solicitud
        public DateTime? ApprovedAt { get; set; }         // fecha_aprobacion

        // ================================
        //      Navigation Properties
        // ================================
        public Project Project { get; set; } = null!;                     // Navegación a Project
        public Document? Document { get; set; }
    }
}
