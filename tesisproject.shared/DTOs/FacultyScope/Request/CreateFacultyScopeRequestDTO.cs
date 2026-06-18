using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.FacultyScope.Request
{
    public class CreateFacultyScopeRequestDTO
    {
        public string Name { get; set; } = null!;
        public List<int> FacultyIds { get; set; } = new();
    }
}
