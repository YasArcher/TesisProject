using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Enums;

namespace tesisproject.shared.DTOs.VisitIssues.Response
{
    /// <summary>
    /// Read model used for list/detail views.
    /// </summary>
    public class VisitIssueResponseDTO
    {
        public int VisitIssueId { get; set; }
        public int VisitId { get; set; }

        // string Title { get; set; } = null!;
        public string? Description { get; set; }
        //public string? ActionPlan { get; set; }

        public IssueStatus Status { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? ResolvedDate { get; set; }

        public int? ReportedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
