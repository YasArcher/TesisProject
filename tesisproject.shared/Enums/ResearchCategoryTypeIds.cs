using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Enums
{
    /// <summary>
    /// ResearchCategoryType IDs are hardcoded and must match the seeded catalog in DB.
    /// These records should be locked (IsLocked = 1).
    /// </summary>
    public static class ResearchCategoryTypeIds
    {
        public const int Dominio = 1;
        public const int LineaInvestigacion = 2;
        public const int SubLineaInvestigacion = 3;

        public const int CampoAmplio = 4;
        public const int CampoEspecifico = 5;
        public const int CampoDetallado = 6;

        public const int AlcanceTerritorial = 7;
        public const int ImpactoEsperado = 8;
    }
}