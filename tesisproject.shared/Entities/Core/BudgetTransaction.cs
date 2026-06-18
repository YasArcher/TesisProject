using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.Entities.Core
{
    public class BudgetTransaction
    {
        // ================================
        //              Keys
        // ================================
        public int BudgetTransactionId { get; set; }

        // ================================
        //          Foreign Keys
        // ================================
        [Required]
        public int BudgetId { get; set; }

        [Required]
        public int TransactionTypeId { get; set; }

        [Required]
        public int CertifiedByUserId { get; set; }

        public int? ExecutedByUserId { get; set; }

        // ================================
        //           Amounts
        // ================================
        /// <summary>
        /// Certified amount for this budget item.
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal CertifiedAmount { get; set; }

        /// <summary>
        /// Executed amount (devengado). Must be <= CertifiedAmount.
        /// Null or 0 when not executed yet.
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal? ExecutedAmount { get; set; }

        // ================================
        //           Dates
        // ================================
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required, Column(TypeName = "date")]
        public DateTime CertifiedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "date")]
        public DateTime? ExecutedAt { get; set; }

        // ================================
        //        Extra Info
        // ================================
        [StringLength(200)]
        public string BudgetItem { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CURNumber { get; set; }

        // NUEVOS CAMPOS:
        [StringLength(1000)]
        public string CertificationDescription { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? ExecutionDescription { get; set; }

        // ================================
        //        Navigation Properties
        // ================================
        public Budget Budget { get; set; } = null!;
        public TransactionType TransactionType { get; set; } = null!;
    }
}
