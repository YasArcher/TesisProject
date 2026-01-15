using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Group.Response
{
    /// <summary>
    /// Respuesta para historial de integrantes de un proyecto (tipo reporte),
    /// agrupado por Resolución/Extensión.
    /// </summary>
    public class ProjectMembersHistoryResponseDTO
    {
        public int ProjectId { get; set; }

        /// <summary>
        /// Bloques del reporte (por resolución).
        /// </summary>
        public IReadOnlyList<ProjectMembersHistoryResolutionBlockDTO> Blocks { get; set; }
            = Array.Empty<ProjectMembersHistoryResolutionBlockDTO>();
    }

    /// <summary>
    /// Bloque del reporte: "DURACIÓN APROBADA CON RESOLUCIÓN X (N MESES)".
    /// </summary>
    public class ProjectMembersHistoryResolutionBlockDTO
    {
        /// <summary>
        /// Ej: "UTA-CONIN-2021-0065-R"
        /// </summary>
        public string ResolutionCode { get; set; } = string.Empty;

        /// <summary>
        /// Meses aprobados por esa resolución (si aplica).
        /// Ej: 18
        /// </summary>
        public int? ApprovedDurationMonths { get; set; }

        /// <summary>
        /// Período cubierto por la resolución (si lo necesitas para el encabezado).
        /// </summary>
        public DateTime? ResolutionStart { get; set; }
        public DateTime? ResolutionEnd { get; set; }

        /// <summary>
        /// Filas de integrantes (tabla).
        /// </summary>
        public IReadOnlyList<ProjectMemberParticipationRowDTO> Members { get; set; }
            = Array.Empty<ProjectMemberParticipationRowDTO>();
    }

    /// <summary>
    /// Fila del reporte para un integrante dentro de un bloque (resolución).
    /// Equivale a: Nombre / Designación / Período / Meses / Días.
    /// </summary>
    public class ProjectMemberParticipationRowDTO
    {
        // Identificadores útiles para trazabilidad en frontend
        public int GroupMemberId { get; set; }
        public int AspNetUserId { get; set; } // si lo manejas así en tu dominio

        // Columnas visibles del reporte
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Ej: "COORDINADOR PRINCIPAL", "INVESTIGADOR", etc.
        /// </summary>
        public string DesignationInProject { get; set; } = string.Empty;

        /// <summary>
        /// Fechas base del historial (entrada/salida).
        /// Si LeftAt es null, sigue activo (o hasta la fecha fin del bloque si se calcula así).
        /// </summary>
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }

        /// <summary>
        /// Texto listo para renderizar tipo "octubre-2021/abril-2023".
        /// (Se calcula en el service; el DTO ya lo expone formateado.)
        /// </summary>
        public string ParticipationPeriodLabel { get; set; } = string.Empty;

        /// <summary>
        /// "Número de meses de participación en el proyecto"
        /// (exactamente como tu tabla).
        /// </summary>
        public int ParticipationMonths { get; set; }

        /// <summary>
        /// Días residuales además de los meses (si tu cálculo los entrega).
        /// </summary>
        public int ParticipationDays { get; set; }

        /// <summary>
        /// Indicador útil: true si no tiene salida (LeftAt null) o si aún está vigente.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Opcional: si te sirve para filtros/acciones.
        /// </summary>
        public int MemberRoleId { get; set; }
    }
}