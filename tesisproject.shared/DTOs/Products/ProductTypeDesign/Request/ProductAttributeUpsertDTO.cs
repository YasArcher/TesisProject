using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Enums;

namespace tesisproject.shared.DTOs.Products.ProductTypeDesign.Request
{
    public sealed class ProductAttributeUpsertDTO
    {
        public int Id { get; set; }          // 0 => new
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public ProductAttributeDataType DataType { get; set; }
        public string? Unit { get; set; }
    }
}
