using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Products.Product.Request
{
    public class ProductCreateRequestDTO
    {
        // Null denotes independent production in Unified; project editors still supply their project ID.
        public int? ProjectId { get; set; }

        public int? VisitId { get; set; }

        [Required, StringLength(1024)]
        public string Title { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Description { get; set; }

        [Required]
        public int ProductTypeId { get; set; }

        // Institutional AppUser.IdUser values only; never AuthorId or ExternalResearcherId.
        public List<int>? AuthorUserIds { get; set; }

        // values by AttributeDefinitionId
        public List<ProductValueUpsertDTO>? Values { get; set; }
    }
}
