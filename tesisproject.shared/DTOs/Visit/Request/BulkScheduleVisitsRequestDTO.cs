using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Visit.Request
{
    public class BulkScheduleVisitsRequestDTO
    {
        [Required]
        public DateTime ScheduledDate { get; set; }

        [Required]
        [MinLength(1)]
        public IReadOnlyList<int> ProjectIds { get; set; } = Array.Empty<int>();
    }
}