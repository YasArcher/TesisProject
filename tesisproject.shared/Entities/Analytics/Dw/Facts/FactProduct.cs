using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tesisproject.shared.Entities.Analytics.Dw.Bridges;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;

namespace tesisproject.shared.Entities.Analytics.Dw.Facts
{
    public class FactProduct
    {
        [Key]
        public int FactProductId { get; set; }

        public int ProductId { get; set; }
        public int ProjectId { get; set; }

        [Required]
        [MaxLength(1024)]
        public string Title { get; set; } = string.Empty;

        public string? Doi { get; set; }
        public int? PublicationYear { get; set; }
        public string? IssnIsbn { get; set; }

        public int ProductCount { get; set; }
        public int AuthorCount { get; set; }
        public bool IsActiveFlag { get; set; }

        public int FacultyKey { get; set; }
        public int ProductTypeKey { get; set; }
        public int CreatedDateKey { get; set; }

        // Nueva FK opcional
        public int? IndexingDatabaseKey { get; set; }
        public int? QuartileKey { get; set; }
        public int? JournalKey { get; set; }

        // Navigations
        public DimFaculty Faculty { get; set; } = null!;
        public DimProductType ProductType { get; set; } = null!;

        [ForeignKey(nameof(CreatedDateKey))]
        public DimDate CreatedDate { get; set; } = null!;

        public DimIndexingDatabase? IndexingDatabase { get; set; }
        public DimQuartile? Quartile { get; set; }
        public DimJournal? Journal { get; set; }
        public ICollection<BridgeProductAuthor> Authors { get; set; } = new List<BridgeProductAuthor>();
    }
}
