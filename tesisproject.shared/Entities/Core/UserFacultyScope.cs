using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Entities.Core
{
    public class UserFacultyScope
    {
        public int UserFacultyScopeId { get; set; } // PK simple (autoincremental)

        public int IdentityUserId { get; set; }     // Usuario (FK → Identity)
        public string FacultyId { get; set; } = null!; // Código de facultad (texto)

        public bool IsActive { get; set; } = true;
    }
}