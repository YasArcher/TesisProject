using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Budgets.Request
{
    public class BudgetDTO
    {
        public int BudgetId { get; set; }
        public int ProjectId { get; set; }
        public int ApprovedByUserId { get; set; }
        public decimal InitialAmount { get; set; }

        public int FundingTypeId { get; set; }          // ID del tipo
        public string? FundingTypeName { get; set; }

        public decimal CertifiedAmount { get; set; }
        public decimal ExecutedAmount { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }

    public class BudgetListItemDTO
    {
        public int BudgetId { get; set; }
        public int ProjectId { get; set; }
        public decimal InitialAmount { get; set; }
        public decimal CertifiedAmount { get; set; }
        public decimal ExecutedAmount { get; set; }
        public bool IsApproved => ApprovedAt.HasValue;
        public DateTime? ApprovedAt { get; set; }
    }

    public class CreateBudgetRequestDTO
    {
        [Required]
        public int ProjectId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal InitialAmount { get; set; }

        [Required]
        public int FundingTypeId { get; set; }
    }


    public class UpdateBudgetRequestDTO
    {
        [Range(1, int.MaxValue)]
        public int FundingTypeId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal InitialAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal CertifiedAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ExecutedAmount { get; set; }

        public DateTime? ApprovedAt { get; set; }
    }
}