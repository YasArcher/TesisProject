using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace tesisproject.shared.Entities.External
{
    /// <summary>
    /// Flat distributivo record as returned by the external distributivo endpoint.
    /// </summary>
    public sealed class ExternalTeacherDistributivoModel
    {
        // Distributivo PK
        [JsonPropertyName("id_distributivo")]
        public int DistributivoId { get; set; }

        // Period
        [JsonPropertyName("id_periodo")]
        public int PeriodId { get; set; }

        [JsonPropertyName("Periodo")]
        public string PeriodName { get; set; } = string.Empty;

        // Faculty/Career (resolved)
        [JsonPropertyName("id_facultad")]
        public int FacultyId { get; set; }

        [JsonPropertyName("id_carrera")]
        public int? CareerId { get; set; }

        [JsonPropertyName("Facultad")]
        public string FacultyName { get; set; } = string.Empty;

        [JsonPropertyName("Carrera")]
        public string? CareerName { get; set; }

        [JsonPropertyName("ASP_ID")]
        public int? AspId { get; set; }

        [JsonPropertyName("Identificacion")]
        public string Document { get; set; } = string.Empty;

        [JsonPropertyName("correo")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("usuario_nombre")]
        public string FullName { get; set; } = string.Empty;

        // Workload
        [JsonPropertyName("Horas")]
        public decimal Hours { get; set; }
    }
}