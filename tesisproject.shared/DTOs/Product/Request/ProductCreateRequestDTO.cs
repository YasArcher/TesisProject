using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Product.Request
{
    public class ProductCreateRequestDTO
    {
        [Required]
        public int ProjectId { get; set; }

        public int? VisitId { get; set; }

        [Required, MaxLength(1024)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? Description { get; set; }

        [Required]
        public int ProductTypeId { get; set; }

        /// <summary>List of author user IDs (Identity users).</summary>
        public List<int> AuthorUserIds { get; set; } = new();

        /// <summary>
        /// Attribute values keyed by AttributeDefinitionId.
        /// One entry per required/optional attribute you want to set.
        /// </summary>
        public List<ProductAttributeValueUpsertDTO> AttributeValues { get; set; } = new();
    }
}
