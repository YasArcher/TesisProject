using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Product.Response
{
    public class ProductDetailResponseDTO
    {
        // Core
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int? VisitId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int ProductTypeId { get; set; }
        public string ProductTypeName { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Authors (only Identity user ids here; nombres/facultad vendrán de API externa)
        public List<ProductAuthorResponseDTO> Authors { get; set; } = new();

        // Attribute values joined with definition metadata for render/validation
        public List<ProductValueResponseDTO> Values { get; set; } = new();
    }
}
