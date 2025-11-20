namespace tesisproject.backend.DataWarehouse.Entities
{
    public class DimArticle
    {
        public int ArticleKey { get; set; }   
        public int ArticleId { get; set; }   

        public string? Title { get; set; }
        public string? Doi { get; set; }

        public bool IsOpenAccess { get; set; }
        public bool IsProjectResult { get; set; }
        public bool HasInterculturalComponent { get; set; }

        public int? PageCount { get; set; }
        public string? PublicationUrl { get; set; }

        public string? ProceedingsName { get; set; }
        public string? EventName { get; set; }
        public string? GroupName { get; set; }
        public string? Filiacion { get; set; }
    }
}
