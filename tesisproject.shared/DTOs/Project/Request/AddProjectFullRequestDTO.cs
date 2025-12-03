using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.Budgets.Request;
using tesisproject.shared.DTOs.Document.Response;
using tesisproject.shared.DTOs.Group.Request;
using tesisproject.shared.DTOs.ProjectObjective.Request;

namespace tesisproject.shared.DTOs.Project.Request
{
    public class AddProjectFullRequestDTO
    {
        // ===================================
        //   1) Datos principales del proyecto
        // ===================================
        [Required]
        public AddProjectRequestDTO Project { get; set; } = null!;
        [Required]
        public DocumentResponseDTO ProjectDocumentData { get; set; } = null!;

        // Miembros internos (grupo UTA)
        public List<AddGroupMemberRequestDTO> GroupMembers { get; set; } = new();

        // ===================================
        //   2) Presupuesto asociado al proyecto
        // ===================================
        public List<CreateBudgetRequestDTO> Budgets { get; set; } = new();

        // ===================================
        //   3) Objetivos y sus actividades
        // ===================================
        public List<ProjectObjectiveWithActivitiesRequestDTO> Objectives { get; set; } = new();

        // ===================================
        //   4) Fecha programada de la primera visita
        // ===================================
        public DateTime ScheduledDate { get; set; }

        // ===================================
        //   5) Investigadores externos
        // ===================================
        /// <summary>
        /// Identificadores de investigadores externos que participarán en el proyecto.
        /// </summary>
        public List<int> ExternalResearcherIds { get; set; } = new();
    }
}
