using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.backend.Data.UnifiedEntities.Base;

namespace tesisproject.backend.Data.UnifiedEntities.Catalogs
{
    /// <summary>
    /// Catalog for research product types (e.g., "Scientific Publication", "Regional Production", "Conference Talk").
    /// </summary>
    public class ProductType : CatalogEntityBase
    {
        // Keep it simple: Id, Name, IsActive come from CatalogEntityBase.
        // Add new fields here only if they apply to ALL product types.
    }
}