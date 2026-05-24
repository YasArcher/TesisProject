namespace tesisproject.backend.DataWarehouse.Entities
{
    public class FactArticleIndexing
    {
        public int Id { get; set; }    // PK

        public int ArticleKey { get; set; }
        public DimArticle Article { get; set; } = null!;

        public int IndexingSourceKey { get; set; }
        public DimIndexingSource IndexingSource { get; set; } = null!;

        public int IndexedCount { get; set; } = 1;
    }
}
