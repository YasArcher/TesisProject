using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;
using tesisproject.shared.Entities.Analytics.Dw.Facts;

namespace tesisproject.shared.Entities.Analytics.Dw.Bridges
{
    /// <summary>
    /// Bridge table that links projects with research categories in the DW.
    /// Each row represents an assignment of a project to a research category.
    /// </summary>
    public class BridgeProjectResearchCategory
    {
        [Key]
        public int BridgeProjectResearchCategoryId { get; set; }  // surrogate key in DW

        // Operational project identifier (from dbo.Projects.ProjectId)
        public int ProjectId { get; set; }

        // FK to DimResearchCategory in the DW
        public int ResearchCategoryKey { get; set; }

        // Navigation to dimension
        public DimResearchCategory ResearchCategory { get; set; } = null!;
        public FactProject Project { get; set; } = null!;
    }
}