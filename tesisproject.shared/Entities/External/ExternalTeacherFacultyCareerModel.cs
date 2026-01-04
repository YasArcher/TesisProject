using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace tesisproject.shared.Entities.External
{
    public sealed class ExternalTeacherFacultyCareerModel
    {
        [JsonPropertyName("teacher_faculty_career_id")]
        public int TeacherFacultyCareerId { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("faculty_career_id")]
        public int FacultyCareerId { get; set; }

        [JsonPropertyName("faculty_career_name")]
        public string FacultyCareerName { get; set; } = string.Empty;

        [JsonPropertyName("faculty_career_siglas")]
        public string? FacultyCareerSiglas { get; set; }

        [JsonPropertyName("faculty_id")]
        public int? FacultyId { get; set; }

        [JsonPropertyName("faculty_name")]
        public string? FacultyName { get; set; }

        [JsonPropertyName("faculty_siglas")]
        public string? FacultySiglas { get; set; }
    }
}