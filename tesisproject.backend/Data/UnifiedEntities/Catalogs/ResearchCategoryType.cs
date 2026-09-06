using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.backend.Data.UnifiedEntities.Base;

namespace tesisproject.backend.Data.UnifiedEntities.Catalogs
{
    public class ResearchCategoryType : CatalogEntityBase
    {
        public int ResearchCategoryGroupId { get; set; }
        public ResearchCategoryGroup ResearchCategoryGroup { get; set; } = null!;
        public bool IsFilterEnabled { get; set; } = false;
        public ICollection<ResearchCategory> ResearchCategories { get; set; } = new List<ResearchCategory>();
    }
}
