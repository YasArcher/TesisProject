using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.backend.Data.UnifiedEntities.Base;

namespace tesisproject.backend.Data.UnifiedEntities.Catalogs
{
    public class Country : CatalogEntityBase
    {
        // ================================
        //        Core Information
        // ================================
        public string IsoCode { get; set; } = string.Empty;   // Código ISO de 2 letras (ej. "EC")
        public string IsoAlpha3 { get; set; } = string.Empty; // Código ISO de 3 letras (ej. "ECU")

        // ================================
        //      Navigation Properties
        // ================================
        public ICollection<Institution>? Institutions { get; set; } // Relación inversa con Institution
    }
}
