using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Catalogs;

namespace tesisproject.shared.Entities.Core
{
    public class Visit
    {
        public Guid VisitId { get; set; }                 // id_visita

        [Required, StringLength(50)]
        public string ProjectId { get; set; } = string.Empty;  // id_proyecto (FK -> Project)
        
        [Required]
        public Guid VisitStateId { get; set; }             // id_tipo_visita (FK -> VisitType catálogo)
        
        public Guid? DocumentId { get; set; }             // id_documento (FK -> Document, opcional)

        public DateTime? VisitDate { get; set; }         // fecha de la visita

        [StringLength(500)]
        public string? Notes { get; set; }               // observaciones

        // Navegaciones
        public Project Project { get; set; } = null!;
        public VisitState VisitState { get; set; } = null!; 
        public Document? Document { get; set; } 

    }
}
