using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Data.UnifiedEntities.Core
{
    public class Budget
    {
        // ================================
        //              Keys
        // ================================
        public int BudgetId { get; set; } // id_presupuesto

        // ================================
        //          Foreign Keys
        // ================================
        [Required]
        public int ProjectId { get; set; } // id_proyecto (FK -> Project)

        [Required]
        public int ApprovedByUserId { get; set; } // id_usuario_aprobador (FK -> UserSystem)
        [Required]
        public int FundingTypeId { get; set; } // id_tipo_financiamiento (catálogo)

        // ================================
        //          Financial Data
        // ================================
        [Range(0, double.MaxValue)]
        public decimal InitialAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal CertifiedAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ExecutedAmount { get; set; }

        // ================================
        //              Dates
        // ================================
        public DateTime? ApprovedAt { get; set; }

        // ================================
        //        Navigation Properties
        // ================================
        public Project Project { get; set; } = null!;

        public FundingType FundingType { get; set; } = null!;

        public ICollection<BudgetTransaction> Transactions { get; set; } = new List<BudgetTransaction>();
    }
}
