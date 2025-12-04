using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Analytics.Dw.Facts;

namespace tesisproject.shared.Entities.Analytics.Dw.Dimensions
{
    public class DimProjectState
    {
        [Key]
        public int ProjectStateKey { get; set; }  // Surrogate key in DW

        // Natural key from ProjectStates.Id
        public int ProjectStateId { get; set; }

        public string Name { get; set; } = string.Empty; // FINALIZADO, EN CIERRE, etc.
        public bool IsActive { get; set; }

        // Navigations to facts
        public ICollection<FactProject> Projects { get; set; } = new List<FactProject>();
    }
}
