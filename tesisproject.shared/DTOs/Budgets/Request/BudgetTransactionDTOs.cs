using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Budgets.Request
{
    // Respuesta
    public class BudgetTransactionDTO
    {
        public int BudgetTransactionId { get; set; }
        public int BudgetId { get; set; }
        public int TransactionTypeId { get; set; }   // 1 = Certification
        public decimal Amount { get; set; }
        public DateTime CertifiedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public int CertifiedByUserId { get; set; }
        public int? ExecutedByUserId { get; set; }
        public string BudgetItem { get; set; } = string.Empty;
        public string? CURNumber { get; set; }
        public bool IsExecuted => ExecutedAt.HasValue;
    }

    // Request: Certificar (crear transacción)
    public class AddCertificationRequestDTO
    {
        [Required] public int BudgetId { get; set; }
        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required, StringLength(100)]
        public string BudgetItem { get; set; } = string.Empty;

        public string? CURNumber { get; set; }

        // Si quieres setear una fecha manual, si no, se usa UtcNow.Date
        public DateTime? CertifiedAt { get; set; }

        [Required] public int CertifiedByUserId { get; set; }
    }

    // Request: Devengar/Ejecutar
    public class ExecuteDevengadoRequestDTO
    {
        [Required] public int BudgetTransactionId { get; set; }
        [Required] public int ExecutedByUserId { get; set; }
        public string? CURNumber { get; set; } // si deseas actualizar/adjuntar número
        public DateTime? ExecutedAt { get; set; } // por defecto UtcNow.Date
    }
}
