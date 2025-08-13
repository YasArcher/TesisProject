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
        public Guid BudgetTransactionId { get; set; }  // id_transaccion_presupuestaria
        [Required]
        public Guid BudgetId { get; set; }            // id_presupuesto (FK -> Budget)
        [Required]
        public Guid TransactionTypeId { get; set; }   // id_tipo_transaccion (FK -> TransactionType catálogo)
        [Required]
        public Guid CertifiedByUserId { get; set; }  // id_usuario_certificador (FK -> UserSystem)
        public Guid? ExecutedByUserId { get; set; }  // id_usuario_ejecutor (FK -> UserSystem)
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }         // monto
        [Column(TypeName = "date"), Required]
        public DateTime CertifiedAt { get; set; } = DateTime.UtcNow;// fecha certificación
        [Column(TypeName = "date")]
        public DateTime ExecutedAt { get; set; } // fecha ejecución
        public string BudgetItem { get; set; } = string.Empty; // partida presupuestaria
        public string? CURNumber { get; set; } // número CURN (opcional)

        // Navegaciones
        public Budget Budget { get; set; } = null!; // Navegación a Budget
        public TransactionType TransactionType { get; set; } = null!; // Navegación a TransactionType
    }
}
