using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Enums;

namespace tesisproject.backend.Data.UnifiedEntities.Core
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
