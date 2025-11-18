using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Convocation.Request
{
    public class ConvocationUpdateRequestDTO
    {
        [Required]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(64)]
        public string? Code { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
