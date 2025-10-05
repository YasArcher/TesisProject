using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.Entities.Core
{
    public class ProjectObjective
    {
        // ================================
        //              Keys
        // ================================
        public int Id { get; set; }  // id_objetivo_proyecto

        // ================================
        //           Foreign Keys
        // ================================
        [Required]
        public int ProjectId { get; set; }  // id_proyecto (FK -> Project)

        [Required]
        public int ObjectiveTypeId { get; set; }  // id_tipo_objetivo (FK -> ObjectiveType)

        // ================================
        //        Core Information
        // ================================
        [Required, StringLength(500)]
        public string Objetive { get; set; } = string.Empty;  // descripcion_objetivo

        [Required, StringLength(500)]
        public string Result { get; set; } = string.Empty;    // resultado_esperado

        // ================================
        //      Navigation Properties
        // ================================
        public Project Project { get; set; } = null!;                 // Navegación a Project
        public ObjectiveType ObjectiveType { get; set; } = null!;     // Navegación a ObjectiveType
    }
}
