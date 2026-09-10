using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Data.UnifiedEntities.Core.Products
{
    /// <summary>
    /// Domain entity representing a concrete research product (article, talk, book, etc.).
    /// Scientific publication fields belong to Article; other extensions use ProductValue.
    /// </summary>
    public class Product
    {
        // =============== Keys ===============
        public int Id { get; set; }

        // ============== FKs ===============
        // Null denotes independent scientific production.
        public int? ProjectId { get; set; }

        public int? VisitId { get; set; }    // Optional visit reference

        // =============== Core ===============
        [Required, MaxLength(1024)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? Description { get; set; }

        /// <summary>
        /// Product type (catalog).
        /// </summary>
        [Required]
        public int ProductTypeId { get; set; }

        // Lifecycle
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // =========== Navigations ============

        public Article? Article { get; set; }

        public ProductType? ProductType { get; set; }
        public ICollection<ProductValue>? Values { get; set; }
        public ICollection<ProductAuthor>? Authors { get; set; }

        public Project? Project { get; set; }
        public Visit? Visit { get; set; }
    }
}