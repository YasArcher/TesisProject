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
        public string ProjectName { get; set; } = string.Empty;

        [Required]
        public int ProjectTypeId { get; set; }

        [Required]
        public int ProjectStateId { get; set; }

        public int DurationInMonths { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? TentativeEndDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? RealEndDate { get; set; }

        [Required]
        public int ConvocationId { get; set; }
    }
}
