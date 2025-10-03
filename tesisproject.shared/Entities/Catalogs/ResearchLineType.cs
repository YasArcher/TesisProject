using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Base;

namespace tesisproject.shared.Entities.Catalogs
{
    public class ResearchLineType: CatalogEntityBase
    {
        public int ResearchDomainTypeId { get; set; }

        // Navigation property
        public ResearchDomainType? ResearchDomainType { get; set; }
    }
}
