using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;

namespace tesisproject.shared.Entities.Analytics.Dw.Facts
{
    public class FactBudget
    {
        [Key]
        public int FactBudgetId { get; set; }   // Surrogate key in DW

        // Reference to operational budget and project
        public int BudgetId { get; set; }       // Budgets.BudgetId
        public int ProjectId { get; set; }      // Budgets.ProjectId

        // Measures
        public decimal InitialAmount { get; set; }    // Budgets.InitialAmount
        public decimal CertifiedAmount { get; set; }  // Budgets.CertifiedAmount
        public decimal ExecutedAmount { get; set; }   // Budgets.ExecutedAmount

        // Foreign keys to dimensions
        public int FacultyKey { get; set; }
        public int FundingTypeKey { get; set; }
        public int ApprovedDateKey { get; set; }

        // Navigation properties
        public DimFaculty Faculty { get; set; } = null!;
        public DimFundingType FundingType { get; set; } = null!;

        [ForeignKey(nameof(ApprovedDateKey))]
        public DimDate ApprovedDate { get; set; } = null!;
    }
}