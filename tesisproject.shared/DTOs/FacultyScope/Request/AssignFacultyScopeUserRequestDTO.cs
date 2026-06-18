using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.FacultyScope.Request
{
    public class AssignFacultyScopeUserRequestDTO
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Document { get; set; } = string.Empty;

        public int? AspUserId { get; set; }
    }
}