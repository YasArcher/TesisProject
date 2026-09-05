using System.ComponentModel.DataAnnotations;

namespace tesisproject.backend.Data.Articles.Entities;

public class IndexingSource
{
    public int IndexingSourceId { get; set; }
    [MaxLength(120)] public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public ICollection<ArticleIndexing> ArticleIndexings { get; set; } = new List<ArticleIndexing>();
}
