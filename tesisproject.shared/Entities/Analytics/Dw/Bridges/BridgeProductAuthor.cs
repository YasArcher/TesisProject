using System.ComponentModel.DataAnnotations;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;
using tesisproject.shared.Entities.Analytics.Dw.Facts;

namespace tesisproject.shared.Entities.Analytics.Dw.Bridges;

public class BridgeProductAuthor
{
    [Key]
    public int BridgeProductAuthorId { get; set; }

    public int ProductAuthorId { get; set; }
    public int FactProductId { get; set; }
    public int AuthorKey { get; set; }
    public int? AuthorOrder { get; set; }
    public bool IsPrimaryAuthor { get; set; }
    public string? Participation { get; set; }
    public string? NameSnapshot { get; set; }

    public FactProduct Product { get; set; } = null!;
    public DimAuthor Author { get; set; } = null!;
}
