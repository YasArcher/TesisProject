using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Enums;

namespace tesisproject.shared.DTOs.ProductAttributeDefinition.Request
{
    public class ProductAttributeDefinitionUpsertItemDTO
    {
        /// <summary>
        /// Null for ADD. Non-null for UPDATE/DELETE.
        /// </summary>
        public int? Id { get; set; }

        /// <summary>Mark for deletion when true (other fields ignored on server).</summary>
        public bool IsDeleted { get; set; } = false;

        // Fields used for ADD/UPDATE:
        [Required(AllowEmptyStrings = false), MaxLength(128)]
        public string AttributeName { get; set; } = string.Empty;

        [Required]
        public ProductAttributeDataType DataType { get; set; } = ProductAttributeDataType.Text;

        public bool IsRequired { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;

        [MaxLength(32)]
        public string? Unit { get; set; }
    }
}
