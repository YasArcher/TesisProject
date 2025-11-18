using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Enums;

namespace tesisproject.shared.Entities.Core
{
    /// <summary>
    /// Issue detected during a visit. No document is stored here.
    /// Keep it simple: status, audit, and navigation to Visit.
    /// </summary>
    public class VisitIssue
    {
        // ================================
        //              Key
        // ================================
        public int VisitIssueId { get; set; }                 // PK

        // ================================
        //           Foreign Key
        // ================================
        [Required]
        public int VisitId { get; set; }                      // FK -> Visit

        // ================================
        //        Core Information
        // ================================

        [StringLength(4000)]
        public string? Description { get; set; }              // optional details

        //[StringLength(4000)]
        //public string? ActionPlan { get; set; }               // free text (one or many actions in a single field)

        // ================================
        //            Status
        // ================================
        [Required]
        public IssueStatus Status { get; set; } = IssueStatus.Open; // Open/InProgress/Resolved/WontFix

        // Optional basic scheduling fields (still minimal)
        public DateTime? DueDate { get; set; }
        public DateTime? ResolvedDate { get; set; }

        // ================================
        //            Audit
        // ================================
        /// <summary>
        /// External user ids; we do not model User entity here.
        /// </summary>
        public int? ReportedByUserId { get; set; }            // who reported the issue (optional)

        [Required]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        // ================================
        //      Navigation Properties
        // ================================
        public Visit Visit { get; set; } = null!;
    }
}
