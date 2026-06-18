// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
namespace tesisproject.shared.DTOs.Reports
{
    public class ArticlesByResearchLineDto
    {
        public string ResearchLineName { get; set; } = null!;
        public int Count { get; set; }
    }
}

