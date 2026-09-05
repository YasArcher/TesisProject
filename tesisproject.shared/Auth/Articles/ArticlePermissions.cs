namespace tesisproject.shared.Auth.Articles;

public static class ArticlePermissions
{
    public const string ClaimType = "permission";

    public const string Access = "articles.access";
    public const string Register = "articles.registration";
    public const string List = "articles.list";
    public const string WriteDirectly = "articles.write-directly";
    public const string UseMatrix = "articles.matrix";
    public const string UseBulkImport = "articles.bulk-import";
    public const string UseExternalApis = "articles.external-api";
    public const string TrackWorkflow = "articles.workflow.track";
    public const string ReviewUodide = "articles.workflow.review-uodide";
    public const string ReviewTechnical = "articles.workflow.review-technical";
    public const string ProcessTechnical = "articles.workflow.process";
    public const string ViewReporting = "articles.reporting.view";
    public const string ExportReporting = "articles.reporting.export";
    public const string UseAdvancedReporting = "articles.reporting.advanced";
    public const string ViewIntelligence = "articles.intelligence.view";
    public const string TrainIntelligence = "articles.intelligence.train";
    public const string ManageConfiguration = "articles.configuration";
    public const string ManageCatalogs = "articles.catalogs";
    public const string ManageSecurity = "articles.security";
    public const string ManageRoles = "articles.roles";

    public static readonly string[] All =
    [
        Access,
        Register,
        List,
        WriteDirectly,
        UseMatrix,
        UseBulkImport,
        UseExternalApis,
        TrackWorkflow,
        ReviewUodide,
        ReviewTechnical,
        ProcessTechnical,
        ViewReporting,
        ExportReporting,
        UseAdvancedReporting,
        ViewIntelligence,
        TrainIntelligence,
        ManageConfiguration,
        ManageCatalogs,
        ManageSecurity,
        ManageRoles
    ];
}
