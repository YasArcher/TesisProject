using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace tesisproject.shared.Entities.External
{
    /// <summary>Neutral academic period as returned by the external periods endpoint.</summary>
    public sealed class ExternalAcademicPeriodDTO
    {
        [JsonPropertyName("id_periodo")]
        public int PeriodId { get; set; }

        [JsonPropertyName("nombre")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("fecha_inicio")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("fecha_fin")]
        public DateTime EndDate { get; set; }
    }
}