using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.External
{
    /// <summary>
    /// Represents a Faculty with its child programs.
    /// </summary>
    public class ExternalFacultyDTO
    {
        public int FacultyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Acronym { get; set; }
        public List<ExternalProgramDTO> Programs { get; set; } = new();
    }
}
