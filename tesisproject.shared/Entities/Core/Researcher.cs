using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.Entities.Core
{
    public class Researcher
    {
        // ================================
        //              Keys
        // ================================
        [Key]
        public int Id { get; set; } // id_investigador

        // ================================
        //         External Identity
        // ================================
        [Required]
        public int ActiveDirectoryId { get; set; } // Relación con Active Directory (1:1 con Identity)

        // ================================
        //        Core Information
        // ================================
        [Required, StringLength(20)]
        public string ReginaCode { get; set; } = null!; // Código SENESCYT (REG-XXXX)

        [Required]
        public int ResearcherTypeId { get; set; } // id_tipo_investigador (Auxiliar, Agregado, Principal)

        [Range(1, 4)]
        public int Level { get; set; } // Nivel dentro del tipo (ej. Aux 1-2, Agr 1-3, Pri 1-4)

        public bool IsActive { get; set; } = true; // estado_activo

        // ================================
        //            Audit
        // ================================
        public int CreatedByUserId { get; set; }          // usuario_creacion
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // fecha_creacion
        public int? UpdatedByUserId { get; set; }         // usuario_actualizacion
        public DateTime? UpdatedAt { get; set; }          // fecha_actualizacion

        // ================================
        //      Navigation Properties
        // ================================
        [ForeignKey(nameof(ResearcherTypeId))]
        public ResearcherType ResearcherType { get; set; } = null!; // Navegación a ResearcherType
    }
}
