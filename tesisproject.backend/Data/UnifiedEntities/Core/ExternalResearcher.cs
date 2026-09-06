using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Data.UnifiedEntities.Core
{
    public class ExternalResearcher
    {
        // ================================
        //              Keys
        // ================================
        public int ExternalResearcherId { get; set; } // id_investigador_externo

        // ================================
        //        Core Information
        // ================================
        [Required, StringLength(150)]
        public string FullName { get; set; } = string.Empty; // nombre_completo

        [Required, StringLength(150)]
        public string Email { get; set; } = string.Empty;    // correo_electronico

        [StringLength(20)]
        public string? PhoneNumber { get; set; }             // telefono

        // ================================
        //           Foreign Keys
        // ================================
        public int? InstitutionId { get; set; }              // id_institucion (nullable)

        // ================================
        //     Navigation Properties
        // ================================
        public Institution? Institution { get; set; }        // Navegación a Institution

        // ================================
        //   Collections / Many-to-Many
        // ================================
        public ICollection<ExternalResearcherProject> ExternalResearcherProjects { get; set; } = new List<ExternalResearcherProject>();

    }
}
