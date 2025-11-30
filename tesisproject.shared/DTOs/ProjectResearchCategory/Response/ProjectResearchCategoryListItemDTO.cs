using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.ProjectResearchCategory.Response
{
    public class ProjectResearchCategoryListItemDTO
    {
        public int Id { get; set; }  // <- Alias estándar del proyecto

        public int ProjectId { get; set; }
        public int ResearchCategoryId { get; set; }

        public string? ResearchCategoryName { get; set; }
    }
}
