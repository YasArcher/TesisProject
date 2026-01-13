using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Visit.Response
{
    public class VisitPlannedForExecutionListDTO
    {
        public int VisitId { get; set; }

        // Visit
        public DateTime? VisitDate { get; set; }
        public int VisitStateId { get; set; }
        public string VisitStateName { get; set; } = string.Empty;
        public int VisitNumber { get; set; }

        // Project (extra fields)
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectCode { get; set; } = string.Empty;

        public int FacultyId { get; set; }
    }
}