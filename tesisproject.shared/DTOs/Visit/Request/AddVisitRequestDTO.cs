using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Visit.Request
{
    public class AddVisitRequestDTO
    {
        [Required]
        public int ProjectId { get; set; }

        // Fecha programada de la visita (no ejecutada aún)
        public DateTime? ScheduledDate { get; set; }
    }
}
