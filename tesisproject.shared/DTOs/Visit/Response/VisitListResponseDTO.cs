using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Visit.Response
{
    public class VisitListResponseDTO
    {
        public int VisitId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;

        public int VisitStateId { get; set; }
        public string VisitStateName { get; set; } = string.Empty;

        public int? DocumentId { get; set; }

        public DateTime? VisitDate { get; set; }
        public string? Notes { get; set; }
    }
}
