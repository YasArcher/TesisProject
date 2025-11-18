using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Visit.Request
{
    public class AssignProjectVisitsReviewerDTO
    {
        [Required] public int ProjectId { get; set; }
        [Required] public int PerformedByUserId { get; set; }
    }
}
