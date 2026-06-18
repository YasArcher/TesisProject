namespace tesisproject.backend.Identity;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Analyst = "Analyst";
    public const string Author = "Author";
    public const string WorkflowReviewerUodide = "WorkflowReviewerUodide";
    public const string WorkflowReviewerAreaTecnica = "WorkflowReviewerAreaTecnica";
    public const string WorkflowProcessorAreaTecnica = "WorkflowProcessorAreaTecnica";
    public const string ExternalApiUser = "ExternalApiUser";
    public const string ArticleRegistrationUser = "ArticleRegistrationUser";
    public const string RegistrationMatrixUser = "RegistrationMatrixUser";
    public const string DirectArticleSaveUser = "DirectArticleSaveUser";
    public const string BulkImportUser = "BulkImportUser";
    public const string ReportingViewer = "ReportingViewer";
    public const string ReportingExporter = "ReportingExporter";
    public const string ReportingAdvancedUser = "ReportingAdvancedUser";
    public const string IntelligenceViewer = "IntelligenceViewer";
    public const string IntelligenceTrainer = "IntelligenceTrainer";
    public const string ConfigurationManager = "ConfigurationManager";
    public const string CatalogManager = "CatalogManager";
    public const string WorkflowTrackingUser = "WorkflowTrackingUser";
    public const string SecurityAdministrator = "SecurityAdministrator";
    public const string RoleManager = "RoleManager";

    public static readonly string[] All =
    [
        Admin,
        Analyst,
        Author,
        WorkflowReviewerUodide,
        WorkflowReviewerAreaTecnica,
        WorkflowProcessorAreaTecnica,
        ExternalApiUser,
        ArticleRegistrationUser,
        RegistrationMatrixUser,
        DirectArticleSaveUser,
        BulkImportUser,
        ReportingViewer,
        ReportingExporter,
        ReportingAdvancedUser,
        IntelligenceViewer,
        IntelligenceTrainer,
        ConfigurationManager,
        CatalogManager,
        WorkflowTrackingUser,
        SecurityAdministrator,
        RoleManager
    ];
}
