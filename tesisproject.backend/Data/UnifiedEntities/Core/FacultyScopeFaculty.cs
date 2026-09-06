using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.backend.Data.UnifiedEntities.Core
{
    public class FacultyScopeFaculty
    {
        public int FacultyScopeId { get; set; }
        public int FacultyId { get; set; }

        public bool IsActive { get; set; } = true;

        // NAV
        public FacultyScope FacultyScope { get; set; } = null!;
    }
}
