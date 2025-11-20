using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Catalog.ProjectType.Request
{
    public class AddProjectTypeRequestDTO
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
