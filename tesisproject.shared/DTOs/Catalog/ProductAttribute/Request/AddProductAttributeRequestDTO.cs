using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Enums;

namespace tesisproject.shared.DTOs.Catalog.ProductAttribute.Request
{
    public class AddProductAttributeRequestDTO
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [Required]
        public ProductAttributeDataType DataType { get; set; } = ProductAttributeDataType.Text;

        [MaxLength(32)]
        public string? Unit { get; set; }
    }
}
