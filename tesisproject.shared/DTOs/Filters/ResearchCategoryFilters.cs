using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.Filters
{
    public class ResearchCategoryFilterTypeDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public int ResearchCategoryGroupId { get; set; }
        public string ResearchCategoryGroupName { get; set; } = string.Empty;

        public List<ResearchCategoryItemDTO> Categories { get; set; } = new();
    }


    public class ResearchCategoryItemDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }          // 👈 NUEVO
        public int ResearchCategoryTypeId { get; set; }     // 👈 opcional pero útil
    }
}
