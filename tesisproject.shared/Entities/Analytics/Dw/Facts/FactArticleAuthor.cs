using System.ComponentModel.DataAnnotations;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;

namespace tesisproject.shared.Entities.Analytics.Dw.Facts;

public class FactArticleAuthor
{
    [Key]
    public int FactArticleAuthorId { get; set; }
    public int ProductAuthorId { get; set; }
    public int ArticleKey { get; set; }
    public int AuthorKey { get; set; }
    public int? AuthorOrder { get; set; }
    public bool IsPrimaryAuthor { get; set; }
    [MaxLength(150)] public string? Participation { get; set; }
    [MaxLength(300)] public string? NameSnapshot { get; set; }
    [MaxLength(300)] public string? AffiliationSnapshot { get; set; }

    public DimArticle Article { get; set; } = null!;
    public DimAuthor Author { get; set; } = null!;
}
