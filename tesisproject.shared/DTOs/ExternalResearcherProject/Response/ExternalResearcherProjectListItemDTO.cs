using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.ExternalResearcherProject.Response
{
    public class ExternalResearcherProjectListItemDTO
    {
        public int Id { get; set; }

        [Required]
        public int ExternalResearcherId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required, StringLength(100)]
        public string Role { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        [Required]
        public int CreatedByUserId { get; set; }

        public DateTime? ExitDate { get; set; }

        // Extra info para mostrar en tablas / UI
        public string ExternalResearcherFullName { get; set; } = string.Empty;
        public string ExternalResearcherEmail { get; set; } = string.Empty;
    }
}