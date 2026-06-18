using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Enums;

namespace tesisproject.shared.DTOs.Catalog.ProductAttribute.Request
{
    public class UpdateProductAttributeRequestDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        [Required]
        public ProductAttributeDataType DataType { get; set; }

        [MaxLength(32)]
        public string? Unit { get; set; }

        public bool IsLocked { get; set; }
    }
}
