using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
namespace tesisproject.backend.Data.UnifiedEntities.Authors;
/// <summary>Academic identity with exactly one institutional or external person source.</summary>
public class Author
{
    public int AuthorId { get; set; }
    public int? AppUserId { get; set; }
    public AppUser? AppUser { get; set; }
    public int? ExternalResearcherId { get; set; }
    public ExternalResearcher? ExternalResearcher { get; set; }
    public string? Orcid { get; set; }
    // Legacy external author reference; provider/global uniqueness are not established.
    public string? ExternalAuthorId { get; set; }
    public ICollection<ProductAuthor> Products { get; set; } = new List<ProductAuthor>();
}
