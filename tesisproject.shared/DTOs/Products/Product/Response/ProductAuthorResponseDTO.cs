using tesisproject.shared.Enums;

namespace tesisproject.shared.DTOs.Products.Product.Response;

public class ProductAuthorResponseDTO
{
    // Null only for responses from the legacy Product service, which has no Author identity.
    public int? AuthorId { get; set; }
    public ProductAuthorType AuthorType { get; set; } = ProductAuthorType.Institutional;
    public int? AppUserId { get; set; }
    // Temporary legacy alias. Always AppUser.IdUser, never an external researcher ID.
    public int? UserId { get => AppUserId; set => AppUserId = value; }
    public int? ExternalResearcherId { get; set; }
    public int? AuthorOrder { get; set; }
    public string? Participation { get; set; }
    public bool IsPrimaryAuthor { get; set; }
    public string? ParticipantTypeSnapshot { get; set; }
    public string? AffiliationSnapshot { get; set; }
    public string? NameSnapshot { get; set; }
    public string? IdentificationSnapshot { get; set; }
    public string? EmailSnapshot { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
