using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Enums;

namespace tesisproject.shared.DTOs.ProductAttributeDefinition.Response
{
    public class ProductAttributeDefinitionItemDTO
    {
        public int Id { get; set; }

        public int ProductTypeId { get; set; }

        public string AttributeName { get; set; } = string.Empty; // "ISSN/ISBN", "DOI", ...
        public ProductAttributeDataType DataType { get; set; } = ProductAttributeDataType.Text;
        public bool IsRequired { get; set; }
        public int DisplayOrder { get; set; }
        public string? Unit { get; set; }
    }
}