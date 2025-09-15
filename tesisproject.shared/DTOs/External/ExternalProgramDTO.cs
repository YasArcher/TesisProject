using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.External
{
    /// <summary>
    /// Represents a career/program that belongs to a Faculty.
    /// </summary>
    public class ExternalProgramDTO
    {
        public int ProgramId { get; set; }
        public int FacultyId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
