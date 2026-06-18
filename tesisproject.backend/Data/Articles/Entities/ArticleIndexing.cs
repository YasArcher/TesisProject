// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
namespace tesisproject.backend.Data.Articles.Entities;

    public class ArticleIndexing
    {
        public int ArticleId { get; set; }
        public Article Article { get; set; } = default!;
        public int IndexingSourceId { get; set; }
        public IndexingSource IndexingSource { get; set; } = default!;
    }

