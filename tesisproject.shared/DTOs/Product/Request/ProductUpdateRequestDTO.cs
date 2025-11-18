using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Product.Request
{
    public class ProductUpdateRequestDTO
    {
        [Required]
        public int Id { get; set; }

        [Required, MaxLength(1024)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }

        /// <summary>Optional replacement of the entire author set.</summary>
        public List<int>? AuthorUserIds { get; set; }

        /// <summary>Partial upsert of attribute values.</summary>
        public List<ProductAttributeValueUpsertDTO>? AttributeValues { get; set; }
    }
}
