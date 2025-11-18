using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Product.Response
{
    /// <summary>Value + definition metadata (denormalized para el FrontEnd).</summary>
    public class ProductValueResponseDTO
    {
        public int AttributeDefinitionId { get; set; }
        public string AttributeName { get; set; } = string.Empty; // e.g., "DOI", "Journal"
        public string DataType { get; set; } = "text";            // "text" | "number" | "date" | "url"
        public bool IsRequired { get; set; }
        public int DisplayOrder { get; set; }
        public string? Unit { get; set; }

        public string? Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
