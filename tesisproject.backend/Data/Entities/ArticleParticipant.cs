namespace tesisproject.backend.Data.Entities;

public class ArticleParticipant
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public Article Article { get; set; } = default!;
    public int Index { get; set; } // 1..5
    public string? Identificacion { get; set; }
    public string? Nombre { get; set; }
    public string? Participacion { get; set; }
}
