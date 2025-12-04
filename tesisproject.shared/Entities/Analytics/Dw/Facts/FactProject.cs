using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;

namespace tesisproject.shared.Entities.Analytics.Dw.Facts
{
    public class FactProject
    {
        [Key]
        public int FactProjectId { get; set; }   // Surrogate key in DW

        // Reference to operational project (Projects.ProjectId)
        public int ProjectId { get; set; }

        // Measures
        public int ProjectCount { get; set; }              // Usually 1 per row
        public int DurationInMonths { get; set; }          // From Projects.DurationInMonths
        public decimal ExecutionPercentage { get; set; }   // From Projects.ExecutionPercentage

        // Foreign keys to dimensions
        public int FacultyKey { get; set; }
        public int ProjectStateKey { get; set; }

        public int ApprovalDateKey { get; set; }
        public int StartDateKey { get; set; }
        public int EndDateKey { get; set; }

        // Navigation properties
        public DimFaculty Faculty { get; set; } = null!;
        public DimProjectState ProjectState { get; set; } = null!;

        [ForeignKey(nameof(ApprovalDateKey))]
        public DimDate ApprovalDate { get; set; } = null!;

        [ForeignKey(nameof(StartDateKey))]
        public DimDate StartDate { get; set; } = null!;

        [ForeignKey(nameof(EndDateKey))]
        public DimDate EndDate { get; set; } = null!;
    }
}
