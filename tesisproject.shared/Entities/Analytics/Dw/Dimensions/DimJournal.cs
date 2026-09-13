using System.ComponentModel.DataAnnotations;
using tesisproject.shared.Entities.Analytics.Dw.Facts;

namespace tesisproject.shared.Entities.Analytics.Dw.Dimensions;

public class DimJournal
{
    [Key]
    public int JournalKey { get; set; }

    [Required]
    [MaxLength(450)]
    public string Name { get; set; } = string.Empty;

    public ICollection<FactProduct> Products { get; set; } = new List<FactProduct>();
}
