using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Product.Request
{
    /// <summary>Upsert payload for a single attribute value.</summary>
    public class ProductAttributeValueUpsertDTO
    {
        [Required]
        public int AttributeDefinitionId { get; set; }

        /// <summary>Raw string value (service will validate per DataType: text/number/date/url).</summary>
        [MaxLength(4000)]
        public string? Value { get; set; }
    }
}
