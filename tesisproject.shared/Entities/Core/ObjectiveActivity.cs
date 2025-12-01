using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace tesisproject.shared.Entities.Core
{
    public class ObjectiveActivity
    {
        // ================================
        //              Keys
        // ================================
        public int ObjectiveActivityId { get; set; } // PK

        // ================================
        //           Foreign Keys
        // ================================
        public int ObjectiveId { get; set; }  // FK → ProjectObjective

        // ================================
        //        Core Information
        // ================================

        [StringLength(1000, ErrorMessage = "El resultado no puede exceder 1000 caracteres")]
        public string ActivityResult { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "La acción no puede exceder 1000 caracteres")]
        public string ActionText { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "El monitoreo no puede exceder 1000 caracteres")]

        public bool IsCompleted { get; set; }

        // ================================
        //    Auditoría (Opcional pero recomendado)
        // ================================
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // ================================
        //      Navigation Properties
        // ================================
        // Relación con ProjectObjective (1:N)
        public ProjectObjective Objective { get; set; } = null!;

        // Relación muchos a muchos con Users a través de ObjectiveActivityUser
        public ICollection<ObjectiveActivityUser> ResponsibleUsers { get; set; } = new List<ObjectiveActivityUser>();
    }
}