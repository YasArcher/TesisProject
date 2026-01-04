using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using tesisproject.shared.Common.Json;

namespace tesisproject.shared.Entities.External
{
    /// <summary>Neutral external profile as returned by the external directory.</summary>
    public sealed class ExternalUserProfileModel
    {
        [JsonPropertyName("id_usuario")]
        public int ExternalId { get; set; }

        [JsonPropertyName("nombre")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("cedula")]
        public string Document { get; set; } = string.Empty;

        [JsonPropertyName("celular")]
        public string Phone { get; set; } = string.Empty;

        [JsonPropertyName("correo")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("cargo")]
        public string Position { get; set; } = string.Empty;

        [JsonPropertyName("ASP_ID")]
        public int? AspId { get; set; }

        [JsonPropertyName("careers")]
        [JsonConverter(typeof(JsonStringOrArrayConverter<ExternalTeacherFacultyCareerModel>))]
        public List<ExternalTeacherFacultyCareerModel> Careers { get; set; } = new();
    }
}