using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Entities.Auth
{
    /// <summary>
    /// Bridge table between application users and ASP.NET Identity / external ASP.
    /// 
    /// - IdUser  : internal stable id used by business tables (FK in Project, Budget, Visit, etc.)
    /// - IdLocal : Id in local ASP.NET Identity (before coupling with UTA ASP)
    /// - IdAsp   : Id in external ASP (UTA ASP) when available
    /// </summary>
    public class AppUser
    {
        /// <summary>
        /// Internal user id used by the application domain (FK in your core entities).
        /// </summary>
        public int IdUser { get; set; }          // ID_USER

        /// <summary>
        /// Local ASP.NET Identity user id (IdentityUser<int>.Id) while using local ASP.
        /// After coupling you can overwrite this value with IdAsp.
        /// </summary>
        public int? IdLocal { get; set; }        // ID_LOCAL

        /// <summary>
        /// External ASP user id (university ASP / EXTERNAL_USER).
        /// </summary>
        public int? IdAsp { get; set; }          // ID_ASP
    }
}
