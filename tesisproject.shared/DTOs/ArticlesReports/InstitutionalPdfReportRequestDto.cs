// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
namespace tesisproject.shared.DTOs.Reports;

public sealed class InstitutionalPdfReportRequestDto
{
    public InstitutionalReportingFilterDto Filter { get; set; } = new();
    public List<ReportChartImageDto> Charts { get; set; } = new();
}

public sealed class ReportChartImageDto
{
    public string ChartId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Base64Png { get; set; } = string.Empty;
}

