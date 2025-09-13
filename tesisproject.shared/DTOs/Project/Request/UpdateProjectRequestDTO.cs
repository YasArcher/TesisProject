using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Project.Request
{
    /// <summary>
    /// DTO used to update an existing project.
    /// </summary>
    public class UpdateProjectRequestDTO
    {
        [Required, StringLength(120)]
        public string ProjectName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ProjectObjective { get; set; }

        [StringLength(200)]
        public string? ResearchLine { get; set; }

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
    }
}
