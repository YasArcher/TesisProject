using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.DTOs.ProjectResearchCategory.Response
{
    public class ProjectResearchCategoryDetailDTO
    {
        public int Id { get; set; }  // Alias estándar del proyecto

        public int ProjectId { get; set; }

        public int ResearchCategoryId { get; set; }
        public string? ResearchCategoryName { get; set; }

        public int ResearchCategoryTypeId { get; set; }
        public string? ResearchCategoryTypeName { get; set; }

        public int? ParentCategoryId { get; set; }
        public string? ParentCategoryName { get; set; }
    }
}