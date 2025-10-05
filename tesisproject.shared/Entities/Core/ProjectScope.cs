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
        // ================================
        //              Keys
        // ================================
        public int ProjectScopeId { get; set; }        // id_proyecto_alcance

        // ================================
        //           Foreign Keys
        // ================================
        [Required]
        public int ProjectId { get; set; }             // id_proyecto (FK -> Project)

        [Required]
        public int ScopeTypeId { get; set; }           // id_tipo_alcance (FK -> ScopeType catálogo)

        // ================================
        //        Core Information
        // ================================
        [StringLength(300)]
        public string? Description { get; set; }       // descripción del alcance (opcional)

        // ================================
        //      Navigation Properties
        // ================================
        public Project Project { get; set; } = null!;          // Navegación a Project
        public ScopeType ScopeType { get; set; } = null!;      // Navegación a ScopeType (catálogo)
    }
}
