using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.VisitObjectiveActivityProgress.Request
{
    public class VisitObjectiveActivityProgressItemRequestDTO
    {
        [Required]
        public int ObjectiveActivityId { get; set; }

        [Range(0, 100)]
        public int ProgressPercentage { get; set; }
    }
}
