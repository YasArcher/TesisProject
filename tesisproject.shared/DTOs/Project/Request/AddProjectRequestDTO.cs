using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Project.Request
{
    public sealed class AddProjectRequestDTO
    {
        [Required, StringLength(120)]
        public string ProjectName { get; set; } = string.Empty;

        [Required]
        public int ProjectTypeId { get; set; }
        [Required]
        public int ProjectGroupId { get; set; }
        public int ResearchLine { get; set; }
        public DateTime ApprovalDate { get; set; }
        public DateTime? StartDate { get; set; }
        public string? ProjectCode { get; set; }
        public int CreatedByUserId { get; set; }
        public int ProjectStateId { get; set; }
        public int DurationInMonths { get; set; }
        public int FacultyId { get; set; }
        public int FundingTypeId { get; set; }
    }
}
