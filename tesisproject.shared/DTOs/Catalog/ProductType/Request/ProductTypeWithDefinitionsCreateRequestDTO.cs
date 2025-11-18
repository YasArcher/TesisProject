using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.ProductAttributeDefinition.Request;

namespace tesisproject.shared.DTOs.Catalog.ProductType.Request
{
    public class ProductTypeWithDefinitionsCreateRequestDTO
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Initial attribute definitions for this type.
        /// </summary>
        [MinLength(0)]
        public List<ProductAttributeDefinitionCreateItemDTO> AttributeDefinitions { get; set; } = new();
    }
}
