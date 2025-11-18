using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Base;

namespace tesisproject.shared.Entities.Catalogs
{
    public class ResearchLineType : CatalogEntityBase
    {
        // ================================
        //           Foreign Keys
        // ================================
        public int ResearchDomainTypeId { get; set; } // id_dominio_investigacion (FK -> ResearchDomainType)

        // ================================
        //      Navigation Properties
        // ================================
        public ResearchDomainType? ResearchDomainType { get; set; } // Navegación a ResearchDomainType
        public ICollection<ProjectResearchLine> ProjectResearchLines { get; set; } = new List<ProjectResearchLine>();
    }
}
