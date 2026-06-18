using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Enums
{
    /// <summary>
    /// DocumentType IDs are hardcoded and must match the seeded catalog in DB.
    /// These records should be locked (IsLocked = 1) to avoid breaking business rules.
    /// </summary>
    public static class DocumentTypeIds
    {
        public const int MemorandoInicial = 1;
        public const int MemorandoInformeFinal = 2;

        public const int ResolucionProrroga = 3;
        public const int ResolucionInformeFinal = 4;

        public const int ContratoAuspicio = 5;

        public const int ResolucionVisita = 6;
    }
}