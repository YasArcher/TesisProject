using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.FacultyScope.Response
{
    public class FacultyScopeFacultyItemDTO
    {
        public int FacultyId { get; set; } // Local FK in Unified; legacy semantics unchanged.
        public int? ExternalFacultyId { get; set; } // Populated by Unified for external selections.
        public bool IsActive { get; set; }
    }
}
