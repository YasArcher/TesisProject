using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Analytics.Dw.Facts;

namespace tesisproject.shared.Entities.Analytics.Dw.Dimensions
{
    public class DimIndexingDatabase
    {
        [Key]
        public int IndexingDatabaseKey { get; set; }

        public string Name { get; set; } = string.Empty;  // Scopus, Web of Science, etc.

        // Navigations to facts
        public ICollection<FactProduct> Products { get; set; } = new List<FactProduct>();
    }
}