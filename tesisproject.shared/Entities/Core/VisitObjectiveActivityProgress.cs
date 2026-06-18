using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Entities.Core
{
    public class VisitObjectiveActivityProgress
    {
        public int Id { get; set; }

        public int VisitId { get; set; }
        public int ObjectiveActivityId { get; set; }

        [Range(0, 100)]
        public int ProgressPercentage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Visit Visit { get; set; } = null!;
        public ObjectiveActivity ObjectiveActivity { get; set; } = null!;
    }
}
