using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.backend.Data.UnifiedEntities.Auth;

namespace tesisproject.backend.Data.UnifiedEntities.Core
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
