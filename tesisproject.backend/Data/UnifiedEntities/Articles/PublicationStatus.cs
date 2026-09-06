using System.ComponentModel.DataAnnotations;

namespace tesisproject.backend.Data.UnifiedEntities.Articles;

public class PublicationStatus
{
    public byte PublicationStatusId { get; set; }
    [MaxLength(20)] public string Name { get; set; } = default!;
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
