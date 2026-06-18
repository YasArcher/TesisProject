using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.ObjectiveActivity.Request;

namespace tesisproject.shared.DTOs.ProjectObjective.Request
{
    public class ProjectObjectiveWithActivitiesRequestDTO
    {
        [Range(1, int.MaxValue)]
        public int ObjectiveTypeId { get; set; }

        [Required, StringLength(500)]
        public string Objective { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string Result { get; set; } = string.Empty;
        // % ponderado del objetivo (entero)
        [Required]
        public int WeightedPercentage { get; set; }
        // Actividades asociadas a ESTE objetivo
        public List<AddObjectiveActivityRequestDTO> Activities { get; set; } = new();
    }
}
