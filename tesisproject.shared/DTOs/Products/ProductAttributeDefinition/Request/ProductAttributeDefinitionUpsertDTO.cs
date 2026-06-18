using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Request
{
    public sealed class ProductAttributeDefinitionUpsertDTO
    {
        public int Id { get; set; }              // 0 => new
        public int ProductAttributeId { get; set; }

        public bool IsRequired { get; set; }
        public int DisplayOrder { get; set; }
    }
}
