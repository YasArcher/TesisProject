using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Base;

namespace tesisproject.shared.Entities.Catalogs
{
    public class ResearchCategoryGroup : CatalogEntityBase 
    {
        public ICollection<ResearchCategoryType> Types { get; set; } = new List<ResearchCategoryType>();
    }
}