using System.ComponentModel.DataAnnotations;

namespace tesisproject.backend.Data.Entities;

public class ArticleFile
{
    public int ArticleFileId { get; set; }
    public int ArticleId { get; set; }
    public Article Article { get; set; } = default!;
    [MaxLength(260)] public string FileName { get; set; } = default!;
    [MaxLength(500)] public string? FileUrl { get; set; }
    [MaxLength(64)] public string? Sha256 { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
