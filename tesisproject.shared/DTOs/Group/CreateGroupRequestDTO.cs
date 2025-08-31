using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Group
{
    public class CreateGroupRequestDTO
    {
        [Required]
        public Guid GroupTypeId { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;
    }
}
