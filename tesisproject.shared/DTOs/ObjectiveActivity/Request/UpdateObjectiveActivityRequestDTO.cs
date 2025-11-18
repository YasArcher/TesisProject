using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.ObjectiveActivity.Request
{
    /// <summary>
    /// Request payload for updating an existing ObjectiveActivity.
    /// </summary>
    public class UpdateObjectiveActivityRequestDTO
    {
        [Range(1, int.MaxValue)]
        public int ObjectiveActivityId { get; set; }

        [Range(1, int.MaxValue)]
        public int ObjectiveId { get; set; }

        [StringLength(1000)]
        public string? ActivityResult { get; set; }

        [StringLength(1000)]
        public string? ImprovementAction { get; set; }

        public bool IsCompleted { get; set; }
    }
}