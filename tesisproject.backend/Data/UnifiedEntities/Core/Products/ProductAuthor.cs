using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Data.UnifiedEntities.Authors;
namespace tesisproject.backend.Data.UnifiedEntities.Core.Products;
/// <summary>Authorship of one product, including historical data supplied at registration.</summary>
public class ProductAuthor
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;
    // Optional for products that did not previously record author ordering.
    public int? AuthorOrder { get; set; }
    public string? Participation { get; set; }
    public bool IsPrimaryAuthor { get; set; }
    public string? ParticipantTypeSnapshot { get; set; }
    public string? AffiliationSnapshot { get; set; }
    // Submitted publication byline/contact details survive changes in the current person record.
    public string? NameSnapshot { get; set; }
    public string? IdentificationSnapshot { get; set; }
    public string? EmailSnapshot { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<ProductAuthorDynamicFieldValue> DynamicFieldValues { get; set; }
        = new List<ProductAuthorDynamicFieldValue>();
}
