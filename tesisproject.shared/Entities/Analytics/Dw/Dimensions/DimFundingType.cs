using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Analytics.Dw.Facts;

namespace tesisproject.shared.Entities.Analytics.Dw.Dimensions
{
    public class DimFundingType
    {
        [Key]
        public int FundingTypeKey { get; set; }    // Surrogate key in DW

        // Natural key from FundingTypes.Id
        public int FundingTypeId { get; set; }

        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        // Navigations to facts
        public ICollection<FactBudget> Budgets { get; set; } = new List<FactBudget>();
    }
}
