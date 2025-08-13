using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Entities.Core
{
    public class Budget
    {
        public Guid BudgetId { get; set; }                 // id_presupuesto

        [Required, StringLength(50)]
        public string ProjectId { get; set; } = string.Empty; // id_proyecto (FK -> Project)
        [Required]
        public Guid ApprovedByUserId { get; set; }        // id_usuario_aprobador (FK -> UserSystem)

        [Range(0, double.MaxValue)]
        public decimal InitialAmount { get; set; }        // monto inicial
        [Range(0, double.MaxValue)]
        public decimal CertifiedAmount { get; set; }     // monto certificado
        [Range(0, double.MaxValue)]
        public decimal ExecutedAmount { get; set; }      // monto ejecutado

        public DateTime? ApprovedAt { get; set; }         // fecha aprobación

        // Navegaciones
        public Project Project { get; set; } = null!;    // Navegación a Project
    }
}
