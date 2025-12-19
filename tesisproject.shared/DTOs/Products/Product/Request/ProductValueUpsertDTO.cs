using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Products.Product.Request
{
    public class ProductValueUpsertDTO
    {
        [Required]
        public int AttributeDefinitionId { get; set; }

        public string? Value { get; set; }
    }
}
