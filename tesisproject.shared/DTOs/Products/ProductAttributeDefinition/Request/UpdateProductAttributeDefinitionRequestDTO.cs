using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Request
{
    public class UpdateProductAttributeDefinitionRequestDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int ProductTypeId { get; set; }

        [Required]
        public int ProductAttributeId { get; set; }

        public bool IsRequired { get; set; }

        public int DisplayOrder { get; set; }
    }
}
