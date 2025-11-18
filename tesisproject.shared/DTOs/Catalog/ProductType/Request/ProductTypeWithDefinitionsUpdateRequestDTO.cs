using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.ProductAttributeDefinition.Request;

namespace tesisproject.shared.DTOs.Catalog.ProductType.Request
{
    public class ProductTypeWithDefinitionsUpdateRequestDTO
    {
        [Required]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Attribute definition upserts. 
        /// - If Id is null => ADD
        /// - If Id has value and IsDeleted == true => DELETE
        /// - If Id has value and IsDeleted == false => UPDATE
        /// </summary>
        [MinLength(0)]
        public List<ProductAttributeDefinitionUpsertItemDTO> AttributeDefinitions { get; set; } = new();
    }
}
