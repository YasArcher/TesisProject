using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.backend.Data.UnifiedEntities.Core.Products
{
    /// <summary>
    /// Stores the actual value of a product attribute for a given product.
    /// One value per (Product, ProductAttributeDefinition).
    /// </summary>
    [Index(nameof(ProductId), nameof(AttributeDefinitionId), IsUnique = true)]
    public class ProductValue
    {
        // =============== Keys ===============
        public int Id { get; set; }

        // =============== FKs ===============

        /// <summary>
        /// Concrete product (book, article, chapter, etc.).
        /// </summary>
        [Required]
        public int ProductId { get; set; }

        /// <summary>
        /// Link to the (ProductType, ProductAttribute) assignment.
        /// </summary>
        [Required]
        public int AttributeDefinitionId { get; set; }

        // =============== Value ===============

        /// <summary>
        /// Stored as TEXT to remain flexible across datatypes
        /// (e.g., "Q2", "10.1234/abcd", "2024-10-01", "https://...").
        /// </summary>
        public string? Value { get; set; }

        // Audit (optional)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // =========== Navigations ============

        public Product? Product { get; set; }
        public ProductAttributeDefinition? AttributeDefinition { get; set; }
    }
}