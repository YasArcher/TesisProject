using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.DTOs.Project.Request
{
    /// <summary>
    /// DTO used to update an existing project.
    /// </summary>
    public class UpdateProjectRequestDTO
    {
        [Required, StringLength(120)]
        public string ProjectName { get; set; } = string.Empty;

        public ICollection<int>? ResearchLineTypeIds { get; set; }

        [Required]
        public int ProjectTypeId { get; set; }

        [Required]
        public int ProjectStateId { get; set; }

        [Required]
        public int ProjectGroupId { get; set; }

        public int? SenesytGroupId { get; set; }
        public int? InitialDocumentId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? TentativeEndDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? RealEndDate { get; set; }

        [Range(0, 100)]
        public decimal? ExecutionPercentage { get; set; }

        [Required]
        public int ConvocationId { get; set; }
    }
}
