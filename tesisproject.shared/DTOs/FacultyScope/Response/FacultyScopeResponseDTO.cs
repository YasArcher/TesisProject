using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.FacultyScope.Response
{
    public class FacultyScopeResponseDTO
    {
        public int FacultyScopeId { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }

        public List<FacultyScopeFacultyItemDTO> Faculties { get; set; } = new();
        public List<int> AssignedUserIds { get; set; } = new();
    }
}
