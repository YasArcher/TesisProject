using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Enums
{
    /// <summary>
    /// FundingType IDs are hardcoded and must match the seeded catalog in DB:
    /// 1 = Externo, 2 = Interno, 3 = Propio, 4 = Sin financiamiento.
    /// These records should be locked (IsLocked = 1) to avoid breaking business rules.
    /// </summary>
    public static class FundingTypeIds
    {
        public const int Externo = 1;
        public const int Interno = 2;
        public const int Propio = 3;
        public const int SinFinanciamiento = 4;

        public static readonly int[] All =
        {
            Externo, Interno, Propio, SinFinanciamiento
        };
    }
}