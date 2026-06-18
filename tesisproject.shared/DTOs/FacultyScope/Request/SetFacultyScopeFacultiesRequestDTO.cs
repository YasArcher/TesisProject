using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.FacultyScope.Request
{
    public class SetFacultyScopeFacultiesRequestDTO
    {
        public List<int> FacultyIds { get; set; } = new();
    }
}
