using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Entities.Core.Products
{
    /// <summary>
    /// Stores the actual value of a product attribute (per product).
    /// One value per (Product, AttributeDefinition).
    /// AttributeDefinition is global and may be linked to the Product's type.
    /// </summary>
    [Index(nameof(ProductId), nameof(AttributeDefinitionId), IsUnique = true)]
    public class ProductValue
    {
        // =============== Keys ===============
        public int Id { get; set; }

        // =============== FKs ===============
        [Required]
        public int ProductId { get; set; }

        [Required]
        public int AttributeDefinitionId { get; set; }

        // =============== Value ===============
        // Keep it TEXT to remain flexible across datatypes.
        public string? Value { get; set; }   // e.g., "Q2", "10.1234/abcd", "2024-10-01", "https://..."

        // Audit (optional)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // =========== Navigations ============
        public Product? Product { get; set; }
        public ProductAttributeDefinition? AttributeDefinition { get; set; }
    }
}