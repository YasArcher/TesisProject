using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Analytics.Dw.Facts;

namespace tesisproject.shared.Entities.Analytics.Dw.Dimensions
{
    public class DimDate
    {
        [Key]
        public int DateKey { get; set; }

        public DateTime Date { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }

        public string? PeriodName { get; set; }

        // Navigations to facts
        public ICollection<FactProject> ApprovalProjects { get; set; } = new List<FactProject>();
        public ICollection<FactProject> StartProjects { get; set; } = new List<FactProject>();
        public ICollection<FactProject> EndProjects { get; set; } = new List<FactProject>();

        public ICollection<FactBudget> ApprovedBudgets { get; set; } = new List<FactBudget>();
        public ICollection<FactProduct> CreatedProducts { get; set; } = new List<FactProduct>();
    }
}