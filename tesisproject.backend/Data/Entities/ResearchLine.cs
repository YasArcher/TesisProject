using System.ComponentModel.DataAnnotations;

namespace tesisproject.backend.Data.Entities;

public class ResearchLine
{
    public int ResearchLineId { get; set; }
    [MaxLength(200)] public string Name { get; set; } = default!;
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
