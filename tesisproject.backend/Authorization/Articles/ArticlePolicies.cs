using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using tesisproject.shared.Auth;
using tesisproject.shared.Auth.Articles;

namespace tesisproject.backend.Authorization.Articles;

public static class ArticlePolicies
{
    public static void Configure(AuthorizationOptions options)
    {
        Add(options, ArticlePolicyNames.Access, ArticlePermissions.Access, ArticleRoles.All);
        Add(options, ArticlePolicyNames.AuthorSubmission, ArticlePermissions.Register,
            ArticleRoles.Author, ArticleRoles.Analyst, ArticleRoles.ArticleRegistrationUser,
            ArticleRoles.RegistrationMatrixUser, ArticleRoles.WorkflowReviewerUodide);
        Add(options, ArticlePolicyNames.Listing, ArticlePermissions.List,
            ArticleRoles.Analyst, ArticleRoles.DirectArticleSaveUser, ArticleRoles.ReportingViewer,
            ArticleRoles.ReportingExporter, ArticleRoles.ReportingAdvancedUser,
            ArticleRoles.WorkflowReviewerAreaTecnica, ArticleRoles.WorkflowProcessorAreaTecnica);
        Add(options, ArticlePolicyNames.Write, ArticlePermissions.WriteDirectly,
            ArticleRoles.Analyst, ArticleRoles.DirectArticleSaveUser);
        Add(options, ArticlePolicyNames.WorkflowAccess, ArticlePermissions.TrackWorkflow,
            ArticleRoles.Author, ArticleRoles.WorkflowTrackingUser, ArticleRoles.WorkflowReviewerUodide,
            ArticleRoles.WorkflowReviewerAreaTecnica, ArticleRoles.WorkflowProcessorAreaTecnica);
        Add(options, ArticlePolicyNames.WorkflowReviewUodide, ArticlePermissions.ReviewUodide,
            ArticleRoles.WorkflowReviewerUodide);
        Add(options, ArticlePolicyNames.WorkflowReviewTechnical, ArticlePermissions.ReviewTechnical,
            ArticleRoles.WorkflowReviewerAreaTecnica, ArticleRoles.WorkflowProcessorAreaTecnica);
        Add(options, ArticlePolicyNames.WorkflowProcess, ArticlePermissions.ProcessTechnical,
            ArticleRoles.WorkflowReviewerAreaTecnica, ArticleRoles.WorkflowProcessorAreaTecnica);
        Add(options, ArticlePolicyNames.BulkImport, ArticlePermissions.UseBulkImport,
            ArticleRoles.Analyst, ArticleRoles.BulkImportUser, ArticleRoles.WorkflowReviewerUodide,
            ArticleRoles.WorkflowReviewerAreaTecnica, ArticleRoles.WorkflowProcessorAreaTecnica);
        Add(options, ArticlePolicyNames.ExternalApis, ArticlePermissions.UseExternalApis,
            ArticleRoles.Analyst, ArticleRoles.Author, ArticleRoles.ExternalApiUser);
        Add(options, ArticlePolicyNames.Reporting, ArticlePermissions.ViewReporting,
            ArticleRoles.Analyst, ArticleRoles.ReportingViewer, ArticleRoles.ReportingExporter,
            ArticleRoles.ReportingAdvancedUser, ArticleRoles.WorkflowReviewerAreaTecnica,
            ArticleRoles.WorkflowProcessorAreaTecnica);
        Add(options, ArticlePolicyNames.Intelligence, ArticlePermissions.ViewIntelligence,
            ArticleRoles.Analyst, ArticleRoles.IntelligenceViewer, ArticleRoles.IntelligenceTrainer);
        Add(options, ArticlePolicyNames.Configuration, ArticlePermissions.ManageConfiguration,
            ArticleRoles.Analyst, ArticleRoles.ConfigurationManager, ArticleRoles.CatalogManager);
        Add(options, ArticlePolicyNames.SecurityAdministration, ArticlePermissions.ManageSecurity,
            ArticleRoles.SecurityAdministrator, ArticleRoles.RoleManager);
    }

    private static void Add(
        AuthorizationOptions options,
        string policyName,
        string permission,
        params string[] compatibleRoles)
    {
        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                IsSystemAdministrator(context.User) ||
                context.User.HasClaim(ArticlePermissions.ClaimType, permission) ||
                compatibleRoles.Any(context.User.IsInRole));
        });
    }

    private static bool IsSystemAdministrator(ClaimsPrincipal user)
        => user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.SuperAdmin);
}
