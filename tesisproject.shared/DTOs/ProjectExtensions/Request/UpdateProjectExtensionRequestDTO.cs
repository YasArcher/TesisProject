using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.ProjectExtensions.Request
{
    public class UpdateProjectExtensionRequestDTO
    {
        [Required]
        public int ProjectExtensionId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int ProjectExtensionTypeId { get; set; }

        public int? DocumentId { get; set; }

        public DateTime? RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}