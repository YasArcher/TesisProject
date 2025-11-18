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
    /// Defines which attributes a given ProductType requires (name, datatype, order, etc.).
    /// Used to render dynamic forms and to validate ProductValue.
    /// </summary>
    public class ProductAttributeDefinition
    {
        // =============== Keys ===============
        public int Id { get; set; }

        // =============== FK ===============
        [Required]
        public int ProductTypeId { get; set; }

        // =============== Definition ===============
        [Required, MaxLength(128)]
        public string AttributeName { get; set; } = string.Empty;  // e.g., "ISSN/ISBN", "DOI", "Journal"

        [Required]
        public ProductAttributeDataType DataType { get; set; } = ProductAttributeDataType.Text;

        public bool IsRequired { get; set; } = false;

        /// <summary>Display order for forms.</summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>Optional unit (only meaningful for numeric types).</summary>
        [MaxLength(32)]
        public string? Unit { get; set; }

        // =========== Navigations ============
        public ICollection<ProductValue>? ProductValues { get; set; }
        public ProductType? ProductType { get; set; }
    }
}