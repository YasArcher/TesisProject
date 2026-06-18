using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Products.Product.Response
{
    public class ProductValueResponseDTO
    {
        // identity of the row
        public int AttributeDefinitionId { get; set; }

        // metadata from design
        public int ProductAttributeId { get; set; }
        public string ProductAttributeName { get; set; } = string.Empty;
        public string DataType { get; set; } = "text";
        public string? Unit { get; set; }

        public bool IsRequired { get; set; }
        public int DisplayOrder { get; set; }

        // actual stored value
        public string? Value { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
