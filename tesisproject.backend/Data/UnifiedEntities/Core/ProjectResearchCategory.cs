using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Data.UnifiedEntities.Core
{
    public class ProjectResearchCategory
    {
        public int ProjectResearchCategoryId { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public int ResearchCategoryId { get; set; }
        public ResearchCategory ResearchCategory { get; set; } = null!;
    }
}