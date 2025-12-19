using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Products.Product.Response
{
    public class ProductListItemResponseDTO
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int? VisitId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int ProductTypeId { get; set; }
        public string ProductTypeName { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
