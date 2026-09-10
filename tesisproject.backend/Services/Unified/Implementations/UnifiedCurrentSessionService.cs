using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.Auth.Articles;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations;

public sealed class UnifiedCurrentSessionService(IUnifiedArticleUserContext user,
    IAuthorizationService authorization, IOptions<ArticlesModuleOptions> options) : IUnifiedCurrentSessionService
{
    public async Task<ServiceResult<CurrentSessionResponse>> GetAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        var identityUserId = user.IdentityUserId;
        if (!user.IsAuthenticated || !identityUserId.HasValue)
        {
            return ServiceResult<CurrentSessionResponse>
                .Fail(
                    "La sesion no contiene un identificador de usuario valido.",
                    ErrorType.Unauthorized,
                    "AUTH_USER_ID_MISSING");
        }

        var enabled = options.Value.Enabled;

        async Task<bool> CanAsync(string policy)
            => enabled && (await authorization.AuthorizeAsync(principal, policy)).Succeeded;

        var response = new CurrentSessionResponse
        {
            IdentityUserId = identityUserId.Value,
            AppUserId = await user.GetAppUserIdAsync(ct),
            Email = user.Email,
            DisplayName = user.DisplayName,
            Roles = user.Roles,
            Permissions = user.Permissions,
            Articles = new ArticleModuleAccessResponse
            {
                Enabled = enabled,
                CanAccess = await CanAsync(ArticlePolicyNames.Access),
                CanRegister = await CanAsync(ArticlePolicyNames.AuthorSubmission),
                CanList = await CanAsync(ArticlePolicyNames.Listing),
                CanUseWorkflow = await CanAsync(ArticlePolicyNames.WorkflowAccess),
                CanReviewUodide = await CanAsync(ArticlePolicyNames.WorkflowReviewUodide),
                CanReviewTechnical = await CanAsync(ArticlePolicyNames.WorkflowReviewTechnical),
                CanProcess = await CanAsync(ArticlePolicyNames.WorkflowProcess),
                CanUseBulkImport = await CanAsync(ArticlePolicyNames.BulkImport),
                CanUseExternalApis = await CanAsync(ArticlePolicyNames.ExternalApis),
                CanViewReporting = await CanAsync(ArticlePolicyNames.Reporting),
                CanViewIntelligence = await CanAsync(ArticlePolicyNames.Intelligence),
                CanManageConfiguration = await CanAsync(ArticlePolicyNames.Configuration),
                CanManageSecurity = await CanAsync(ArticlePolicyNames.SecurityAdministration)
            }
        };

        return ServiceResult<CurrentSessionResponse>
            .Ok(response, "Sesion activa.");
    }
}
