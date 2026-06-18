// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
namespace tesisproject.shared.DTOs.Reports;

public sealed class ReportingHealthDto
{
    public bool CanConnect { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string LastEtlStatus { get; set; } = "Sin ejecuciones";
    public DateTime? LastEtlStartedAt { get; set; }
    public DateTime? LastEtlFinishedAt { get; set; }
    public string? LastEtlNotes { get; set; }
    public int DimDateRows { get; set; }
    public int ArticleRows { get; set; }
    public int BatchRows { get; set; }
    public int WorkflowStageRows { get; set; }
}

