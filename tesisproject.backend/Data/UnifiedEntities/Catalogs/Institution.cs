using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.backend.Data.UnifiedEntities.Base;

namespace tesisproject.backend.Data.UnifiedEntities.Catalogs
{
    public class Institution : CatalogEntityBase
    {
        // ================================
        //           Foreign Keys
        // ================================
        public int? CountryId { get; set; } // id_pais (nullable)

        // ================================
        //      Navigation Properties
        // ================================
        public Country? Country { get; set; } // Navegación a Country
    }
}
