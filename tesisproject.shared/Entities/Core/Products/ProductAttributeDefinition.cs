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
    /// Bridge entity between ProductType and ProductAttribute.
    /// It defines how a given attribute is used for a specific ProductType
    /// (required, display order, etc.).
    /// </summary>
    public class ProductAttributeDefinition
    {
        // =============== Keys ===============
        public int Id { get; set; }

        // =============== FKs ===============

        /// <summary>
        /// Product type that uses this attribute.
        /// </summary>
        [Required]
        public int ProductTypeId { get; set; }

        /// <summary>
        /// Global attribute definition (DOI, Pages, Title, etc.).
        /// </summary>
        [Required]
        public int ProductAttributeId { get; set; }

        // =============== Rules per type ===============

        /// <summary>
        /// Whether this attribute is required for this specific ProductType.
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// Display order for UI forms within this ProductType.
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        // =========== Navigations ============

        public ProductType? ProductType { get; set; }
        public ProductAttribute? ProductAttribute { get; set; }

        /// <summary>
        /// Values stored for this attribute when applied to concrete products.
        /// </summary>
        public ICollection<ProductValue>? ProductValues { get; set; }
    }
}