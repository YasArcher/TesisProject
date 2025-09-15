using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Entities.External
{
    /// <summary>
    /// Mirrors the external API payload for faculties and careers in a single list.
    /// </summary>
    public class ExternalFacultyCareerApiModel
    {
        public int id_facultad_carrera { get; set; }
        public int? id_facultad_carrera_pertenece { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string? siglas { get; set; }  // only present for faculties
    }
}
