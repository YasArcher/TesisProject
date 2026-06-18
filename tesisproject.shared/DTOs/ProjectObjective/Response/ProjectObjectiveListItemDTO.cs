using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.ProjectObjective.Response
{
    /// <summary>
    /// Lightweight DTO for listing project objectives.
    /// </summary>
    public class ProjectObjectiveListItemDTO
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }

        public int ObjectiveTypeId { get; set; }

        [Required, StringLength(100)]
        public string ObjectiveTypeName { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string Objective { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string Result { get; set; } = string.Empty;

        /// <summary>
        /// Number of activities linked to this objective (optional, for UI).
        /// </summary>
        public int ActivitiesCount { get; set; }
    }
}