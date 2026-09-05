namespace tesisproject.shared.DTOs.Auth;

public sealed class CurrentSessionResponse
{
    public int IdentityUserId { get; init; }
    public int? AppUserId { get; init; }
    public string? Email { get; init; }
    public string? DisplayName { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Permissions { get; init; } = Array.Empty<string>();
    public ArticleModuleAccessResponse Articles { get; init; } = new();
}

public sealed class ArticleModuleAccessResponse
{
    public bool Enabled { get; init; }
    public bool CanAccess { get; init; }
    public bool CanRegister { get; init; }
    public bool CanList { get; init; }
    public bool CanUseWorkflow { get; init; }
    public bool CanReviewUodide { get; init; }
    public bool CanReviewTechnical { get; init; }
    public bool CanProcess { get; init; }
    public bool CanUseBulkImport { get; init; }
    public bool CanUseExternalApis { get; init; }
    public bool CanViewReporting { get; init; }
    public bool CanViewIntelligence { get; init; }
    public bool CanManageConfiguration { get; init; }
    public bool CanManageSecurity { get; init; }
}
