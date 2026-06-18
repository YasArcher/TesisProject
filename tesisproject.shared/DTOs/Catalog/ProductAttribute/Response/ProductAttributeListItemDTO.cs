using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Enums;

namespace tesisproject.shared.DTOs.Catalog.ProductAttribute.Response
{
    public class ProductAttributeListItemDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }

        public ProductAttributeDataType DataType { get; set; }
        public string? Unit { get; set; }

    }
}
