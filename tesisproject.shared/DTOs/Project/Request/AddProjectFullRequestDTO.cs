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

        // Se envía la colección de miembros del grupo
        public List<AddGroupMemberRequestDTO> GroupMembers { get; set; } = new();

        // ===================================
        //   2) Presupuesto asociado al proyecto
        // ===================================
        public CreateBudgetRequestDTO? Budget { get; set; }

        // ===================================
        //   3) Objetivos y sus actividades
        // ===================================
        public List<ProjectObjectiveWithActivitiesRequestDTO> Objectives { get; set; } = new();

        // ===================================
        //   4) Fecha programada de la primera visita
        // ===================================
        public DateTime ScheduledDate { get; set; }
    }
}