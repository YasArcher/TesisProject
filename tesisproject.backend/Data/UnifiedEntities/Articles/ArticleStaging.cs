namespace tesisproject.backend.Data.UnifiedEntities.Articles;

// Article-owned staging remains separate from the project aggregate and final Products.
public sealed class ArticleStagingBatch
{
    public int ArticleStagingBatchId { get; set; }
    public int RegistrationMatrixId { get; set; }
    public RegistrationMatrix Matrix { get; set; } = null!;
    public string? CreatedByUserId { get; set; }
    public string Status { get; set; } = "Staged";
    public int TotalRows { get; set; }
    public int ReadyRows { get; set; }
    public int DuplicateRows { get; set; }
    public int InvalidRows { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ArticleStagingRow> Rows { get; set; } = new List<ArticleStagingRow>();
}

public sealed class ArticleStagingRow
{
    public int ArticleStagingRowId { get; set; }
    public int ArticleStagingBatchId { get; set; }
    public ArticleStagingBatch Batch { get; set; } = null!;
    public int RegistrationMatrixRowId { get; set; }
    public int RowNumber { get; set; }
    public string Status { get; set; } = "Ready";
    public string? DoiNormalized { get; set; }
    public string SnapshotJson { get; set; } = "{}";
    public string? ValidationMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
