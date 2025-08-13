using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.Entities.Core
{
    public class ProjectScope
    {
        public Guid ProjectScopeId { get; set; }        // id_proyecto_alcance

        [Required, StringLength(50)]
        public string ProjectId { get; set; } = string.Empty;  // id_proyecto (FK -> Project)

        [Required]
        public Guid ScopeTypeId { get; set; }           // id_tipo_alcance (FK -> ScopeType catálogo)

        [StringLength(300)]
        public string? Description { get; set; }       // descripción del alcance (opcional)
        //Navegaciones
        public Project Project { get; set; } = null!; // Navegación a Project
        public ScopeType ScopeType { get; set; } = null!; // Navegación a ScopeType (catálogo)

    }
}
