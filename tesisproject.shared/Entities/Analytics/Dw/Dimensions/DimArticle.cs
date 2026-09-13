using System.ComponentModel.DataAnnotations;

namespace tesisproject.shared.Entities.Analytics.Dw.Dimensions;

public class DimArticle
{
    [Key]
    public int ArticleKey { get; set; }

    public int ProductId { get; set; }
    public int? ArticleId { get; set; }
    public int ProductTypeId { get; set; }

    [Required, MaxLength(1024)]
    public string Title { get; set; } = string.Empty;

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    [MaxLength(450)] public string? Doi { get; set; }
    public short? PublicationYear { get; set; }
    [MaxLength(50)] public string? YearRaw { get; set; }
    [MaxLength(100)] public string? IssnIsbn { get; set; }
    [MaxLength(2048)] public string? PublicationUrl { get; set; }

    [MaxLength(50)] public string? ExternalSource { get; set; }
    [MaxLength(150)] public string? ExternalId { get; set; }
    [MaxLength(300)] public string? ProceedingsName { get; set; }
    [MaxLength(300)] public string? Proceedings { get; set; }
    [MaxLength(300)] public string? EventName { get; set; }
    [MaxLength(300)] public string? GroupName { get; set; }
    [MaxLength(300)] public string? Filiacion { get; set; }
}
