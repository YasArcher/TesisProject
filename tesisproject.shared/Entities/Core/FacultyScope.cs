using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Entities.Core
{
    public class FacultyScope
    {
        public int FacultyScopeId { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; } = true;

        // NAVS
        public ICollection<FacultyScopeFaculty> Faculties { get; set; } = new List<FacultyScopeFaculty>();
        public ICollection<UserFacultyScopeAssignment> UserAssignments { get; set; } = new List<UserFacultyScopeAssignment>();
    }
}
