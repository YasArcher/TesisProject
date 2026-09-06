using System.ComponentModel.DataAnnotations;

namespace tesisproject.backend.Data.UnifiedEntities.Articles;

public class Faculty
{
    public int FacultyId { get; set; }
    public int? ExternalFacultyId { get; set; }
    public DateTime? LastSyncedAt { get; set; }

    [MaxLength(40)]
    public string? Acronym { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
