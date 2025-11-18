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
    /// Payload to create a new VisitIssue.
    /// </summary>
    public class VisitIssueCreateRequestDTO
    {
        [Required]
        public int VisitId { get; set; }
        [StringLength(4000)]
        public string? Description { get; set; }

        //[StringLength(4000)]
        //public string? ActionPlan { get; set; }

        /// <summary>
        /// Optional at creation. Defaults to Open in the service if null.
        /// </summary>
        public IssueStatus? Status { get; set; }  // default to Open server-side if null

        public DateTime? DueDate { get; set; }

        /// <summary>
        /// External user id who reported the issue (optional).
        /// </summary>
        public int? ReportedByUserId { get; set; }
    }
}