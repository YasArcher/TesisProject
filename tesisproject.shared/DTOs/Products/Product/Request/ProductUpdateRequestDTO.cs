using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Products.Product.Request
{
    public class ProductUpdateRequestDTO
    {
        [Required]
        public int Id { get; set; }

        [Required, StringLength(1024)]
        public string Title { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }

        // if null => do not touch, if empty => remove all
        public List<int>? AuthorUserIds { get; set; }

        // if null => do not touch, if empty => clear all values
        public List<ProductValueUpsertDTO>? Values { get; set; }
    }
}
