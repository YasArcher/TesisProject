using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Analytics.Dw.Facts;

namespace tesisproject.shared.Entities.Analytics.Dw.Dimensions
{
    public class DimFaculty
    {
        [Key]
        public int FacultyKey { get; set; }   // Surrogate key in DW

        // Natural key coming from Projects.FacultyId (API external source)
        public int FacultyId { get; set; }

        public string? FacultyCode { get; set; }
        public string? FacultyName { get; set; }

        // Navigations to facts
        public ICollection<FactProject> Projects { get; set; } = new List<FactProject>();
        public ICollection<FactBudget> Budgets { get; set; } = new List<FactBudget>();
        public ICollection<FactProduct> Products { get; set; } = new List<FactProduct>();
    }
}