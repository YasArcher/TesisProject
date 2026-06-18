using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.FacultyScope.Request
{
    public class UpdateFacultyScopeRequestDTO
    {
        public string Name { get; set; } = null!;
        public bool? IsActive { get; set; } // opcional
    }
}
