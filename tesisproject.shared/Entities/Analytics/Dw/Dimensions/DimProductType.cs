using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Analytics.Dw.Facts;

namespace tesisproject.shared.Entities.Analytics.Dw.Dimensions
{
    public class DimProductType
    {
        [Key]
        public int ProductTypeKey { get; set; }    // Surrogate key in DW

        // Natural key from ProductTypes.Id
        public int ProductTypeId { get; set; }

        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        // Navigations to facts
        public ICollection<FactProduct> Products { get; set; } = new List<FactProduct>();
    }
}
