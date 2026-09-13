using System.ComponentModel.DataAnnotations;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;

namespace tesisproject.shared.Entities.Analytics.Dw.Facts;

public class FactArticleIndexing
{
    [Key]
    public int FactArticleIndexingId { get; set; }
    public int ArticleKey { get; set; }
    public int IndexingSourceKey { get; set; }

    public DimArticle Article { get; set; } = null!;
    public DimIndexingSource IndexingSource { get; set; } = null!;
}
