using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.ObjectiveActivityUser.Request
{
    /// <summary>
    /// Request payload for assigning a user to an ObjectiveActivity in a given Visit.
    /// </summary>
    public class AssignObjectiveActivityUserRequestDTO
    {
        [Range(1, int.MaxValue)]
        public int ObjectiveActivityId { get; set; }

        [Range(1, int.MaxValue)]
        public int UserId { get; set; }

        [Range(1, int.MaxValue)]
        public int VisitId { get; set; }

        [Range(0, double.MaxValue)]
        public double? WeeklyHours { get; set; }

        [StringLength(500)]
        public string? RoleDescription { get; set; }

        [StringLength(1000)]
        public string? ReportNotes { get; set; }
    }
}