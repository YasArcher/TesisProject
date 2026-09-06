using System.ComponentModel.DataAnnotations;

namespace tesisproject.backend.Data.UnifiedEntities.Articles;

public class AcademicTerm
{
    public int AcademicTermId { get; set; }
    public int? ExternalPeriodId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    [MaxLength(100)] public string Name { get; set; } = default!;
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
