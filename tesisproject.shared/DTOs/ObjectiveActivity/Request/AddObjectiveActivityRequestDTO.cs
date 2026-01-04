using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.ObjectiveActivity.Request
{
    /// <summary>
    /// Request payload for creating a new ObjectiveActivity.
    /// </summary>
    public class AddObjectiveActivityRequestDTO
    {
        [Range(1, int.MaxValue)]
        public int? ObjectiveId { get; set; }

        [StringLength(1000)]
        public string? ActivityResult { get; set; }

        [StringLength(1000)]
        public string? ActionText { get; set; }

        /// <summary>
        /// Optional initial progress (0..100). Defaults to 0.
        /// </summary>
        [Range(0, 100)]
        public int ProgressPercentage { get; set; } = 0;
    }
}