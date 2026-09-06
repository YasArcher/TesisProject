using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.backend.Data.UnifiedEntities.Core
{
    /// <summary>
    /// Join entity that represents the participation of an external researcher in a specific project.
    /// Includes metadata such as role, creation info, and exit date.
    /// </summary>
    public class ExternalResearcherProject
    {
        // ================================
        //              Keys
        // ================================
        [Key]
        public int ExternalResearcherProjectId { get; set; } // Primary Key

        // ================================
        //           Foreign Keys
        // ================================

        [Required]
        public int ExternalResearcherId { get; set; }   // FK -> ExternalResearcher

        [Required]
        public int ProjectId { get; set; }              // FK -> Project

        // ================================
        //          Participation Info
        // ================================
        [Required, StringLength(100)]
        public string Role { get; set; } = string.Empty;   // e.g., "Advisor", "Co-Author", "Consultant"

        [DataType(DataType.DateTime)]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow; // fecha_creacion

        [Required]
        public int CreatedByUserId { get; set; }           // id_usuario_creador

        [DataType(DataType.Date)]
        public DateTime? ExitDate { get; set; }            // fecha_salida (nullable)

        // ================================
        //      Navigation Properties
        // ================================
        public ExternalResearcher ExternalResearcher { get; set; } = null!;
        public Project Project { get; set; } = null!;
    }
}