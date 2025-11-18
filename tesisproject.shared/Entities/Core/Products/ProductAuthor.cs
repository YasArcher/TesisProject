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
    /// Many-to-many link between Product and ASP.NET Identity User.
    /// Stores authorship metadata (order, role, faculty, career, etc.).
    /// </summary>
    [Index(nameof(ProductId), nameof(UserId), IsUnique = true)]
    public class ProductAuthor
    {
        public int Id { get; set; }

        // ===============================
        //        Foreign Keys
        // ===============================
        [Required]
        public int ProductId { get; set; }

        /// <summary>
        /// ID of the ASP.NET Identity user (Author).
        /// </summary>
        [Required]
        public int UserId { get; set; }

        // ===============================
        //        Audit Fields
        // ===============================
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        // =============================== Navigations ===============================
        public Product? Product { get; set; }
    }
}