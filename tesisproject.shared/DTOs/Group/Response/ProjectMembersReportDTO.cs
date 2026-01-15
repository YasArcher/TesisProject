using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Group.Response
{
    public class ProjectMembersReportDTO
    {
        public int ProjectId { get; set; }
        public IReadOnlyList<ProjectMembersReportSectionDTO> Sections { get; set; }
            = Array.Empty<ProjectMembersReportSectionDTO>();
    }

    public class ProjectMembersReportSectionDTO
    {
        // Ej: "DURACIÓN APROBADA CON RESOLUCIÓN ... (18 MESES)"
        public string Title { get; set; } = string.Empty;

        // Para renderizar "octubre-2021/abril-2023" en el encabezado si quieres
        public string CoveredPeriod { get; set; } = string.Empty;

        public IReadOnlyList<ProjectMemberReportRowDTO> Rows { get; set; }
            = Array.Empty<ProjectMemberReportRowDTO>();
    }

    public class ProjectMemberReportRowDTO
    {
        public string FullName { get; set; } = string.Empty;              // NOMBRE
        public string DesignationInProject { get; set; } = string.Empty;  // DESIGNACIÓN EN EL PROYECTO
        public string ParticipationPeriod { get; set; } = string.Empty;   // "octubre-2021/abril-2023"
        public int Months { get; set; }                                   // Meses
        public int Days { get; set; }                                     // Días
    }
}