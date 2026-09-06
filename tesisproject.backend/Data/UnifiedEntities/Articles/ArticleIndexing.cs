using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
namespace tesisproject.backend.Data.UnifiedEntities.Articles;

    public class ArticleIndexing
    {
        public int ArticleId { get; set; }
        public Article Article { get; set; } = default!;
        public int IndexingSourceId { get; set; }
        public IndexingSource IndexingSource { get; set; } = default!;
    }
