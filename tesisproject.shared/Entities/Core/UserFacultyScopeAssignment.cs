using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Auth;

namespace tesisproject.shared.Entities.Core
{
    public class UserFacultyScopeAssignment
    {
        public int IdentityUserId { get; set; }
        public int FacultyScopeId { get; set; }

        public bool IsActive { get; set; } = true;

        // NAVS
        public AppUser User { get; set; } = null!;
        public FacultyScope FacultyScope { get; set; } = null!;
    }
}
