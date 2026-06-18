// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
namespace tesisproject.shared.DTOs.Articles
{
    public class UpdateArticleRequest : CreateArticleRequest
    {
        public int Id { get; set; }
    }
}

