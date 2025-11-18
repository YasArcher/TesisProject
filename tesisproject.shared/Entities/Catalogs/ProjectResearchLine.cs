using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Core;

namespace tesisproject.shared.Entities.Catalogs
{
    public class ProjectResearchLine
    {
        public int ProjectResearchLineId { get; set; }
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public int ResearchLineTypeId { get; set; }
        public ResearchLineType ResearchLineType { get; set; } = null!;
    }
}
