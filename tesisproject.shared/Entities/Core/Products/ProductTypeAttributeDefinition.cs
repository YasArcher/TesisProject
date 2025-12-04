using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.Entities.Core.Products
{
    /// <summary>
    /// Links a global ProductAttributeDefinition with a specific ProductType,
    /// controlling requirement and display order for that type.
    /// </summary>
    public class ProductTypeAttributeDefinition
    {
        // =============== Keys ===============
        public int Id { get; set; }

        // =============== FKs ===============
        [Required]
        public int ProductTypeId { get; set; }

        [Required]
        public int AttributeDefinitionId { get; set; }

        // =============== Per-type settings ===============
        /// <summary>
        /// Whether this attribute is required for this specific ProductType.
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// Display order for forms within this ProductType.
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        // =========== Navigations ============
        public ProductType? ProductType { get; set; }
        public ProductAttributeDefinition? AttributeDefinition { get; set; }
    }
}