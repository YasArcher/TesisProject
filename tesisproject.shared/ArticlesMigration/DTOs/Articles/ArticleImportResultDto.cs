namespace tesisproject.shared.DTOs.Articles
{
    public class ArticleImportResultDto
    {
        public int TotalRows { get; set; }         
        public int Processed { get; set; }         
        public int Created { get; set; }           
        public int Updated { get; set; }            
        public int Errors { get; set; }            
        public List<string> ErrorMessages { get; set; } = new();
    }
}
