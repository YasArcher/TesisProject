using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.Entities.Core
{
    public class Researcher
    {
        [Key]
        public int Id { get; set; }

        // --- Relación con Active Directory (1:1 con Identity) ---
        [Required]
        public int ActiveDirectoryId { get; set; }

        // --- Datos propios del investigador ---
        [Required]
        [MaxLength(20)]
        public string ReginaCode { get; set; } = null!; // Código SENESCYT (REG-XXXX)

        [Required]
        public int ResearcherTypeId { get; set; } // Auxiliar, Agregado, Principal

        [Range(1, 4)]
        public int Level { get; set; } // Nivel dentro del tipo (ej. Aux 1-2, Agr 1-3, Pri 1-4)

        public bool IsActive { get; set; } = true;

        // --- Auditoría ---
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? UpdatedByUserId { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // --- Navegaciones ---
        [ForeignKey(nameof(ResearcherTypeId))]
        public ResearcherType ResearcherType { get; set; } = null!;
    }
}
