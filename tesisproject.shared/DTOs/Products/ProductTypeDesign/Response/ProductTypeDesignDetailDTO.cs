using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.Catalog.Common.Response;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Response;
using tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Response;

namespace tesisproject.shared.DTOs.Products.ProductTypeDesign.Response
{
    public sealed class ProductTypeDesignDetailDTO
    {
        /// <summary>
        /// Product type being edited/created.
        /// </summary>
        public CatalogDetailDTO ProductType { get; set; } = default!;

        /// <summary>
        /// All attributes that can be used in definitions (existing or newly created).
        /// </summary>
        public IReadOnlyList<ProductAttributeDetailDTO> Attributes { get; set; }
            = Array.Empty<ProductAttributeDetailDTO>();

        /// <summary>
        /// Attribute definitions for this product type.
        /// </summary>
        public IReadOnlyList<ProductAttributeDefinitionDetailDTO> Definitions { get; set; }
            = Array.Empty<ProductAttributeDefinitionDetailDTO>();
    }
}
