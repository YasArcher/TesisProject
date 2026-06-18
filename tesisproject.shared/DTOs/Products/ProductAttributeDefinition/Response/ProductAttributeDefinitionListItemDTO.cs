using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Enums;

namespace tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Response
{
    public class ProductAttributeDefinitionListItemDTO
    {
        public int Id { get; set; }

        public int ProductTypeId { get; set; }
        public string ProductTypeName { get; set; } = string.Empty;

        public int ProductAttributeId { get; set; }
        public string ProductAttributeName { get; set; } = string.Empty;

        public ProductAttributeDataType DataType { get; set; }
        public string? Unit { get; set; }

        public bool IsRequired { get; set; }
        public int DisplayOrder { get; set; }
    }
}
