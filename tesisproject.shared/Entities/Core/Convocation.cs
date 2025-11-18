using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Entities.Core
{
    public class Convocation
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(64)]
        public string? Code { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<ConvocationRule>? Rules { get; set; }
    }
}
