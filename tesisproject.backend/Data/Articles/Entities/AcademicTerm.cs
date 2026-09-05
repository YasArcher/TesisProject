using System.ComponentModel.DataAnnotations;

namespace tesisproject.backend.Data.Articles.Entities;

public class AcademicTerm
{
    public int AcademicTermId { get; set; }
    [MaxLength(100)] public string Name { get; set; } = default!;
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
