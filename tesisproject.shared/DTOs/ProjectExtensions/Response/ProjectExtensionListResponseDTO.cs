using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.ProjectExtensions.Response
{
    public class ProjectExtensionListResponseDTO
    {
        public int ProjectExtensionId { get; set; }

        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;

        public int ProjectExtensionTypeId { get; set; }
        public string ProjectExtensionTypeName { get; set; } = string.Empty;

        public int? DocumentId { get; set; }
        public string? DocumentName { get; set; }

        public DateTime? RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}
