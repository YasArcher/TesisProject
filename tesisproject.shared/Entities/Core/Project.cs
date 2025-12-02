using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Entities.Core.Products;

namespace tesisproject.shared.Entities.Core
{
    public class Project
    {
        // ================================
        //            Keys / Codes
        // ================================
        public int ProjectId { get; set; } 

        [StringLength(20)]
        public string ProjectCode { get; set; } = string.Empty; // código_proyecto

        // ================================
        //            Foreign Keys
        // ================================
        [Required]
        public int CreatedByUserId { get; set; }
        [Required]
        public int ProjectTypeId { get; set; }
        [Required]
        public int ProjectStateId { get; set; }
        [Required]
        public int ProjectGroupId { get; set; }
        public int? InitialDocumentId { get; set; }

        // ================================
        //          Core Information
        // ================================
        public string ProjectName { get; set; } = string.Empty;
        [Required]
        public int ProjectNumber { get; set; } = 0;
        [Required]
        public int ConvocationId { get; set; } = 0;

        // ================================
        //              Dates
        // ================================
        [Column(TypeName = "date")]
        public DateTime ApprovalDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime? StartDate { get; set; }

        public int DurationInMonths { get; set; }

        [Column(TypeName = "date")]
        public DateTime? TentativeEndDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime? RealEndDate { get; set; }

        // ================================
        //             Metrics
        // ================================
        [Range(0, 100)]
        public decimal? ExecutionPercentage { get; set; }

        [Required]
        public int FacultyId { get; set; }

        // ================================
        //        Navigation Properties
        // ================================
        public ProjectType ProjectType { get; set; } = null!;
        public ProjectState ProjectState { get; set; } = null!;
        public Group ProjectGroup { get; set; } = null!;
        public Document? InitialDocument { get; set; }
        public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
        public ICollection<ProjectObjective> ProjectObjectives { get; set; } = null!;

        // ================================
        //      Collections / Many-to-Many
        // ================================
        public ICollection<ExternalResearcherProject> ExternalResearcherProjects { get; set; } = new List<ExternalResearcherProject>();
        public ICollection<Visit> Visits { get; set; } = new List<Visit>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public Convocation Convocation { get; set; } = null!;
        public ICollection<ProjectResearchCategory> ProjectResearchCategories { get; set; } = new List<ProjectResearchCategory>();
    }
}
