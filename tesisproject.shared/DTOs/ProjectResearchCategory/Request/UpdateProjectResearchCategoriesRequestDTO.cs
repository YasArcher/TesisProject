using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.ProjectResearchCategory.Request
{
    public class UpdateProjectResearchCategoriesRequestDTO
    {
        public int ProjectId { get; set; }
        public List<int> ResearchCategoryIds { get; set; } = new();
    }
}