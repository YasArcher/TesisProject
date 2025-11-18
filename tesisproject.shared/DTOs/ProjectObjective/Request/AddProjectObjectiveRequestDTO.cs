using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.ProjectObjective.Request
{
    /// <summary>
    /// Request payload for creating a new ProjectObjective.
    /// </summary>
    public class AddProjectObjectiveRequestDTO
    {
        [Range(1, int.MaxValue)]
        public int ProjectId { get; set; }

        [Range(1, int.MaxValue)]
        public int ObjectiveTypeId { get; set; }

        [Required, StringLength(500)]
        public string Objective { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string Result { get; set; } = string.Empty;
    }
}
