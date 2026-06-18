namespace tesisproject.backend.DataWarehouse.Entities
{
    public class FactArticleAuthor
    {
        public int Id { get; set; } // PK

        public int ArticleKey { get; set; }
        public DimArticle Article { get; set; } = null!;

        public int AuthorKey { get; set; }
        public DimAuthor Author { get; set; } = null!;

        public int AuthorIndex { get; set; }  
        public int TotalAuthors { get; set; }  
    }
}
