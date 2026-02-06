using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Enums
{
    /// <summary>
    /// MemberRoleType IDs must match seeded catalog in DB.
    /// </summary>
    public static class MemberRoleTypeIds
    {
        public const int Coordinador = 1;
        public const int Subrogante = 2;
        public const int Investigador = 3;
        public const int InvestigadorGrupo = 4;
    }
}