using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Visit.Request
{
    public class UpdateVisitRequestDTO
    {
        [Required]
        public int VisitId { get; set; }
        [Required]
        public int ProjectId { get; set; }
        [Required]
        public int VisitStateId { get; set; }
        public int? DocumentId { get; set; }
        public DateTime? VisitDate { get; set; }
    }
}
