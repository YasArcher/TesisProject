using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Base;
using tesisproject.shared.Entities.Core.Products;

namespace tesisproject.shared.Entities.Catalogs
{
    /// <summary>
    /// Catalog for research product types (e.g., "Scientific Publication", "Regional Production", "Conference Talk").
    /// </summary>
    public class ProductType : CatalogEntityBase
    {
        // Keep it simple: Id, Name, IsActive come from CatalogEntityBase.
        // Add new fields here only if they apply to ALL product types.

        /// <summary>
        /// Attribute definitions assigned to this product type.
        /// </summary>
        public ICollection<ProductTypeAttributeDefinition> AttributeDefinitions { get; set; }
            = new List<ProductTypeAttributeDefinition>();
    }
}