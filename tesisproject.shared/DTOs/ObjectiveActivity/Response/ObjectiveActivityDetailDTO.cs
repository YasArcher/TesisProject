using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.ObjectiveActivity.Response
{
    /// <summary>
    /// Detailed DTO for a single ObjectiveActivity.
    /// </summary>
    public class ObjectiveActivityDetailDTO
    {
        public int ObjectiveActivityId { get; set; }

        public int ObjectiveId { get; set; }

        [StringLength(1000)]
        public string ActivityResult { get; set; } = string.Empty;

        [StringLength(1000)]
        public string ImprovementAction { get; set; } = string.Empty;

        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}