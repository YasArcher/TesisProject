using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Enums;

namespace tesisproject.shared.Entities.Core.Products
{
    /// <summary>
    /// Global definition of a product attribute (name, datatype, unit).
    /// Can be assigned to one or many ProductTypes via ProductTypeAttributeDefinition.
    /// </summary>
    public class ProductAttributeDefinition
    {
        // =============== Keys ===============
        public int Id { get; set; }

        // =============== Definition ===============
        [Required, MaxLength(128)]
        public string AttributeName { get; set; } = string.Empty;  // e.g., "ISSN/ISBN", "DOI", "Journal"

        [Required]
        public ProductAttributeDataType DataType { get; set; } = ProductAttributeDataType.Text;

        /// <summary>
        /// Optional unit (only meaningful for numeric types).
        /// </summary>
        [MaxLength(32)]
        public string? Unit { get; set; }

        // =========== Navigations ============
        /// <summary>
        /// Links to product types that use this attribute.
        /// </summary>
        public ICollection<ProductTypeAttributeDefinition> ProductTypeLinks { get; set; }
            = new List<ProductTypeAttributeDefinition>();

        /// <summary>
        /// Values assigned to concrete products for this attribute.
        /// </summary>
        public ICollection<ProductValue> ProductValues { get; set; }
            = new List<ProductValue>();
    }
}