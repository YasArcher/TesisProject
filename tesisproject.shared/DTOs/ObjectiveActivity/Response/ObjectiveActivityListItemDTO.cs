using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.ObjectiveActivity.Response
{
    /// <summary>
    /// Lightweight DTO for listing activities of a project objective.
    /// </summary>
    public class ObjectiveActivityListItemDTO
    {
        public int ObjectiveActivityId { get; set; }

        public int ObjectiveId { get; set; }

        [StringLength(1000)]
        public string ActivityResult { get; set; } = string.Empty;

        [StringLength(1000)]
        public string ActionText { get; set; } = string.Empty;

        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Number of assigned users (from ObjectiveActivityUser).
        /// </summary>
        public int ResponsibleUsersCount { get; set; }
    }
}