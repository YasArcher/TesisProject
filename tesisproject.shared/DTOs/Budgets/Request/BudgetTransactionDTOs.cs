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
        [Required(ErrorMessage = "Debe seleccionar un presupuesto.")]
        public int BudgetId { get; set; }

        // Nombre de la transacción (ej. "Certificación de gastos varios")
        [Required(ErrorMessage = "Ingrese el nombre de la transacción.")]
        [StringLength(200, ErrorMessage = "El nombre de la transacción no puede superar los 200 caracteres.")]
        public string Name { get; set; } = string.Empty;

        // Monto certificado
        [Required(ErrorMessage = "Ingrese el monto a certificar.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a certificar debe ser mayor a 0.")]
        public decimal CertifiedAmount { get; set; }

        // Partida presupuestaria asociada al certificado
        [Required(ErrorMessage = "Ingrese la partida presupuestaria.")]
        [StringLength(100, ErrorMessage = "La partida presupuestaria no puede superar los 100 caracteres.")]
        public string BudgetItem { get; set; } = string.Empty;

        // Descripción / detalle de la certificación
        [Required(ErrorMessage = "Ingrese una breve descripción de la certificación.")]
        [StringLength(1000, ErrorMessage = "La descripción de la certificación no puede superar los 1000 caracteres.")]
        public string CertificationDescription { get; set; } = string.Empty;

        public DateTime? CertifiedAt { get; set; }
    }


    // Request: Devengar/Ejecutar
    public class ExecuteDevengadoRequestDTO
    {
        // Transacción que ya tiene la certificación creada
        [Required(ErrorMessage = "Debe seleccionar la certificación que desea devengar.")]
        public int BudgetTransactionId { get; set; }

        // Monto devengado (puede ser menor al certificado, pero > 0)
        [Required(ErrorMessage = "Ingrese el monto a devengar.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a devengar debe ser mayor a 0.")]
        public decimal ExecutedAmount { get; set; }

        [Required(ErrorMessage = "Ingrese el número de CUR.")]
        [StringLength(100, ErrorMessage = "El número de CUR no puede superar los 100 caracteres.")]
        public string CURNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingrese una breve descripción de la ejecución.")]
        [StringLength(1000, ErrorMessage = "La descripción de la ejecución no puede superar los 1000 caracteres.")]
        public string ExecutionDescription { get; set; } = string.Empty;

        public DateTime? ExecutedAt { get; set; }
    }
}