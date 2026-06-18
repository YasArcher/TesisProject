using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.ObjectiveActivity.Response;

namespace tesisproject.shared.DTOs.ProjectObjective.Response
{
    /// <summary>
    /// DTO for listing project objectives with their activities.
    /// </summary>
    public class ProjectObjectiveWithActivitiesDTO : ProjectObjectiveDetailDTO
    {
        /// <summary>
        /// Activities associated to this objective.
        /// </summary>
        public IReadOnlyList<ObjectiveActivityListItemDTO> Activities { get; set; }
            = new List<ObjectiveActivityListItemDTO>();
    }
}
