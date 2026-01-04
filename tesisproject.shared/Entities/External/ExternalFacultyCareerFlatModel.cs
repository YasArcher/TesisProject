using System.Text.Json.Serialization;

namespace tesisproject.shared.Entities.External
{
    /// <summary>
    /// Mirrors the external API payload for faculties and careers in a single list.
    /// </summary>
    public sealed class ExternalFacultyCareerFlatModel
    {
        [JsonPropertyName("id_facultad_carrera")]
        public int Id { get; set; }

        [JsonPropertyName("id_facultad_carrera_pertenece")]
        public int? ParentId { get; set; }  // null => Faculty, not null => Career/Program

        [JsonPropertyName("nombre")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("siglas")]
        public string? Acronym { get; set; }  // only present for faculties
    }
}
