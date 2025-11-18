using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Enums;

namespace tesisproject.shared.DTOs.VisitIssues.Request
{
    /// <summary>
    /// Partial update (PATCH-like). Only non-null properties will be applied.
    /// </summary>
    public class VisitIssueUpdateRequestDTO
    {

        [StringLength(4000)]
        public string? Description { get; set; }

        [StringLength(4000)]
        public string? ActionPlan { get; set; }

        public IssueStatus? Status { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? ResolvedDate { get; set; }

        /// <summary>
        /// External user id who reported the issue (optional).
        /// </summary>
        public int? ReportedByUserId { get; set; }
    }
}