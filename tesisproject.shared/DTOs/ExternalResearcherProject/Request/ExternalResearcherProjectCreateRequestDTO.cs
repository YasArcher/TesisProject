using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.ExternalResearcherProject.Request
{
    public class ExternalResearcherProjectCreateRequestDTO
    {
        [Required]
        public int ExternalResearcherId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required, StringLength(100)]
        public string Role { get; set; } = string.Empty;

        public DateTime? ExitDate { get; set; }
    }
}