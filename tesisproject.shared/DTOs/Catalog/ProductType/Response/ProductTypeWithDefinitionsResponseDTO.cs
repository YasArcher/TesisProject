using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.ProductAttributeDefinition.Response;

namespace tesisproject.shared.DTOs.Catalog.ProductType.Response
{
    public class ProductTypeWithDefinitionsResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public List<ProductAttributeDefinitionItemDTO> AttributeDefinitions { get; set; } = new();
    }
}
