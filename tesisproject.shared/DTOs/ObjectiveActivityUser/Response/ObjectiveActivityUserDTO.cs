using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.ObjectiveActivityUser.Response
{
    /// <summary>
    /// DTO representing an assignment of a user to an ObjectiveActivity and Visit.
    /// </summary>
    public class ObjectiveActivityUserDTO
    {
        public int Id { get; set; }

        public int ObjectiveActivityId { get; set; }

        public int UserId { get; set; }

        public int VisitId { get; set; }

        [Range(0, double.MaxValue)]
        public double? WeeklyHours { get; set; }

        [StringLength(500)]
        public string? RoleDescription { get; set; }

        [StringLength(1000)]
        public string? ReportNotes { get; set; }
    }
}