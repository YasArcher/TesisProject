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
        public int TransactionTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal CertifiedAmount { get; set; }
        public decimal ExecutedAmount { get; set; } // 0 si aún no se ha devengado

        public DateTime CertifiedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }

        public int CertifiedByUserId { get; set; }
        public int? ExecutedByUserId { get; set; }

        public string BudgetItem { get; set; } = string.Empty;
        public string? CURNumber { get; set; }

        public string CertificationDescription { get; set; } = string.Empty;
        public string? ExecutionDescription { get; set; }
    }


    // Request: Certificar (crear transacción)
    public class AddCertificationRequestDTO
    {
        [Required]
        public int BudgetId { get; set; }

        // Nombre de la transacción (ej. "Certificación de gastos varios")
        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        // Monto certificado
        [Required, Range(0.01, double.MaxValue)]
        public decimal CertifiedAmount { get; set; }

        // Partida presupuestaria asociada al certificado
        [Required, StringLength(100)]
        public string BudgetItem { get; set; } = string.Empty;

        // Descripción / detalle de la certificación
        [Required, StringLength(1000)]
        public string CertificationDescription { get; set; } = string.Empty;
    }


    // Request: Devengar/Ejecutar
    public class ExecuteDevengadoRequestDTO
    {
        // Transacción que ya tiene la certificación creada
        [Required]
        public int BudgetTransactionId { get; set; }

        // Monto devengado (puede ser menor al certificado, pero > 0)
        [Required, Range(0.01, double.MaxValue)]
        public decimal ExecutedAmount { get; set; }

        [Required, StringLength(100)]
        public string CURNumber { get; set; } = string.Empty;

        [Required, StringLength(1000)]
        public string ExecutionDescription { get; set; } = string.Empty;
    }

}
