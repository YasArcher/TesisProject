using tesisproject.shared.DTOs.Reports;

namespace tesisproject.frontend.Services.Implementations;

public sealed class InstitutionalReportingPageStateStore
{
    private static readonly TimeSpan FreshWindow = TimeSpan.FromMinutes(10);

    public InstitutionalReportingDashboardDto? InstitutionalDashboard { get; set; }
    public AuthorReportingDashboardDto? AuthorDashboard { get; set; }
    public InstitutionalReportingFilterDto Filter { get; set; } = new();
    public string ActiveTab { get; set; } = "overview";
    public string? ActivePreset { get; set; }
    public string? InstitutionalError { get; set; }
    public string? AuthorError { get; set; }
    public bool AuthorDashboardUsingFallback { get; set; }
    public DateTime? LastLoadedAt { get; set; }

    public bool HasFreshInstitutionalDashboard =>
        InstitutionalDashboard is not null
        && LastLoadedAt.HasValue
        && DateTime.Now - LastLoadedAt.Value <= FreshWindow;

    public void ClearAuthorState()
    {
        AuthorDashboard = null;
        AuthorDashboardUsingFallback = false;
        AuthorError = null;
    }

    public void Clear()
    {
        InstitutionalDashboard = null;
        AuthorDashboard = null;
        Filter = new InstitutionalReportingFilterDto();
        ActiveTab = "overview";
        ActivePreset = null;
        InstitutionalError = null;
        AuthorError = null;
        AuthorDashboardUsingFallback = false;
        LastLoadedAt = null;
    }
}
