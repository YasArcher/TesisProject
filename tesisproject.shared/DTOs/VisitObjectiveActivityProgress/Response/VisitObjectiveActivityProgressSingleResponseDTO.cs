using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.VisitObjectiveActivityProgress.Response
{
    public class VisitObjectiveActivityProgressSingleResponseDTO
    {
        public int Id { get; set; }
        public int VisitId { get; set; }
        public int ProjectId { get; set; }

        public int ObjectiveActivityId { get; set; }
        public int ProgressPercentage { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal? ProjectProgressInVisit { get; set; }
        public decimal? CurrentProjectProgress { get; set; }
    }
}