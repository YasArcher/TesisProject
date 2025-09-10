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
        public int ProjectExtensionId { get; set; }       // id_prorroga

        [Required, StringLength(50)]
        public int ProjectId { get; set; }  // id_proyecto (FK -> Project)
        [Required]
        public int ProjectExtensionTypeId { get; set; } // id_tipo_prorroga (FK -> ProjectExtensionType catálogo)

        public int? DocumentId { get; set; }              // id_documento (respaldo, opcional)

        public DateTime? RequestedAt { get; set; }        // fecha solicitud
        public DateTime? ApprovedAt { get; set; }         // fecha aprobación
        // Navegaciones
        public Project Project { get; set; } = null!;     // Navegación a Project
        public Document? Document { get; set; }           // Navegación a Document (opcional)
        public ProjectExtensionType? ProjectExtensionType { get; set; } // tipo de prórroga (FK -> ProjectExtensionType)

    }
}
