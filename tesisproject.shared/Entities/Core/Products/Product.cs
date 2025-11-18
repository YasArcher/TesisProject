using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.Entities.Core.Products
{
    /// <summary>
    /// Domain entity representing a concrete research product (article, talk, book, etc.).
    /// Specific fields (ISSN/ISBN, DOI, Journal, etc.) are stored in ProductValue.
    /// </summary>
    public class Product
    {
        // =============== Keys ===============
        public int Id { get; set; }
        // ============== FKs ===============
        [Required]
        public int ProjectId { get; set; }   // FK → Project (existing entity in Core)
        public int? VisitId { get; set; }   // FK → Visit (existing entity in Core)

        // =============== Core ===============
        [Required, MaxLength(1024)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? Description { get; set; }

        // Product Type (catalog)
        [Required]
        public int ProductTypeId { get; set; }

        // Lifecycle
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // =========== Navigations ============
        public ICollection<ProductValue>? Values { get; set; }
        public ProductType? ProductType { get; set; }
        public ICollection<ProductAuthor>? Authors { get; set; }
        public Project? Project { get; set; }
        public Visit? Visit { get; set; }

    }
}
