using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Budgets.Request
{
    public class UpdateBudgetTransactionRequestDTO
    {
        [Range(1, int.MaxValue)]
        public int TransactionTypeId { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal CertifiedAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ExecutedAmount { get; set; }

        [StringLength(200)]
        public string BudgetItem { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CURNumber { get; set; }

        [StringLength(1000)]
        public string CertificationDescription { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? ExecutionDescription { get; set; }
    }
}