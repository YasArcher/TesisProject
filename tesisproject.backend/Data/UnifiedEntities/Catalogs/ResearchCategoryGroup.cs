using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.backend.Data.UnifiedEntities.Base;

namespace tesisproject.backend.Data.UnifiedEntities.Catalogs
{
    public class ResearchCategoryGroup : CatalogEntityBase 
    {
        public ICollection<ResearchCategoryType> Types { get; set; } = new List<ResearchCategoryType>();
    }
}