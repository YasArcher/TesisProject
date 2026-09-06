using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.backend.Data.UnifiedEntities.Base;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.shared.Enums;

namespace tesisproject.backend.Data.UnifiedEntities.Catalogs
{
    /// <summary>
    /// Global catalog for product attributes (e.g., "DOI", "ISSN/ISBN",
    /// "Pages", "Quartile"). These attributes can be reused across multiple
    /// ProductTypes.
    /// </summary>
    public class ProductAttribute : CatalogEntityBase
    {
        /// <summary>
        /// Data type for this attribute (Text, Number, Date, Url, etc.).
        /// </summary>
        [Required]
        public ProductAttributeDataType DataType { get; set; } = ProductAttributeDataType.Text;

        /// <summary>
        /// Optional unit for numeric attributes (e.g., "pages", "%", "USD").
        /// </summary>
        [MaxLength(32)]
        public string? Unit { get; set; }

        // ===================== Navigations =====================

        /// <summary>
        /// Assignments of this attribute to specific product types.
        /// </summary>
        public ICollection<ProductAttributeDefinition>? TypeDefinitions { get; set; }

        // Values are reached through TypeDefinitions -> ProductValues.
        // A direct collection would create an unrelated shadow ProductAttributeId FK.
    }
}
