using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Request;

namespace tesisproject.shared.DTOs.Products.ProductTypeDesign.Request
{
    public sealed class SaveProductTypeDesignRequestDTO
    {
        /// <summary>
        /// Product type to create or update.
        /// If Id == 0 => create new; otherwise update.
        /// </summary>
        public ProductTypeUpsertDTO ProductType { get; set; } = default!;

        /// <summary>
        /// Attributes to create or update.
        /// You can send only the attributes used in Definitions.
        /// </summary>
        public List<ProductAttributeUpsertDTO> Attributes { get; set; } = new();

        /// <summary>
        /// Full list of definitions for this product type.
        /// The service will sync: create/update/delete based on this list.
        /// </summary>
        public List<ProductAttributeDefinitionUpsertDTO> Definitions { get; set; } = new();
    }
}
