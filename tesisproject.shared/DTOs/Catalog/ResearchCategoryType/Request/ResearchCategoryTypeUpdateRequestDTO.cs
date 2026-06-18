using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Catalog.ResearchCategoryType.Request
{
    public class ResearchCategoryTypeUpdateRequestDTO
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        [Required]
        public int ResearchCategoryGroupId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
