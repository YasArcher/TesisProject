using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Enums
{
    /// <summary>
    /// Visit state IDs are hardcoded and must match the seeded catalog in DB:
    /// 1 = Planned, 2 = Pending, 3 = Realized, 4 = OnHold.
    /// These records must be locked (IsLocked = 1) to avoid breaking business rules.
    /// </summary>
    public static class VisitStateIds
    {
        public const int Planned = 1;
        public const int Pending = 2;
        public const int Realized = 3;
        public const int OnHold = 4;

        /// <summary>
        /// States considered "open" (a project should not create a new visit while any open visit exists).
        /// </summary>
        public static readonly int[] OpenStates = { Planned, Pending, OnHold };
    }
}