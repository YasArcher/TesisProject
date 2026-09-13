using System.ComponentModel.DataAnnotations;
using tesisproject.shared.Entities.Analytics.Dw.Bridges;

namespace tesisproject.shared.Entities.Analytics.Dw.Dimensions;

public class DimAuthor
{
    [Key]
    public int AuthorKey { get; set; }

    public int AuthorId { get; set; }
    public bool IsInstitutional { get; set; }
    public int? AppUserId { get; set; }
    public int? IdAsp { get; set; }
    public int? ExternalResearcherId { get; set; }
    public string? ExternalFullName { get; set; }
    public string? Orcid { get; set; }

    public ICollection<BridgeProductAuthor> ProductLinks { get; set; } = new List<BridgeProductAuthor>();
}
