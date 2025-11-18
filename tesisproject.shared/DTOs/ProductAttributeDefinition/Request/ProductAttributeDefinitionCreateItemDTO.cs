using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Enums;

namespace tesisproject.shared.DTOs.ProductAttributeDefinition.Request
{
    public class ProductAttributeDefinitionCreateItemDTO
    {
        [Required, MaxLength(128)]
        public string AttributeName { get; set; } = string.Empty; // e.g. "DOI", "Journal"

        [Required]
        public ProductAttributeDataType DataType { get; set; } = ProductAttributeDataType.Text;

        public bool IsRequired { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;

        [MaxLength(32)]
        public string? Unit { get; set; }
    }
}
