using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tesisproject.shared.Entities.Core
{
    public class ObjectiveActivity
    {
        public int ObjectiveActivityId { get; set; } // PK

        public int ObjectiveId { get; set; }  // FK → ProjectObjective

        [StringLength(1000, ErrorMessage = "El resultado no puede exceder 1000 caracteres")]
        public string ActivityResult { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "La acción no puede exceder 1000 caracteres")]
        public string ActionText { get; set; } = string.Empty;

        // Progreso 0..100 (parcial)
        [Range(0, 100, ErrorMessage = "El progreso debe estar entre 0 y 100")]

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ProjectObjective Objective { get; set; } = null!;
        public ICollection<ObjectiveActivityUser> ResponsibleUsers { get; set; } = new List<ObjectiveActivityUser>();
        public ICollection<VisitObjectiveActivityProgress> VisitProgresses { get; set; }
    = new List<VisitObjectiveActivityProgress>();
    }
}
