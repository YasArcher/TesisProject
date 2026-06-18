using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Visit.Request
{
    public class FinalizeVisitRequestDTO
    {
        [Required]
        public int VisitId { get; set; }

        // para que no inventemos IDs: el front manda el estado final
        [Required]
        public int FinalVisitStateId { get; set; }
    }
}