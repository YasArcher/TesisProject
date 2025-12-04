using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Analytics.Dw.Facts;

namespace tesisproject.shared.Entities.Analytics.Dw.Dimensions
{
    public class DimQuartile
    {
        [Key]
        public int QuartileKey { get; set; }   // Surrogate key in DW

        /// <summary>
        /// Quartile code, e.g. "Q1", "Q2", "Q3", "Q4".
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Optional description, if you want to add more context later.
        /// </summary>
        [MaxLength(100)]
        public string? Description { get; set; }

        // Navigation to facts (1 quartile → N products)
        public ICollection<FactProduct> Products { get; set; } = new List<FactProduct>();
    }
}
