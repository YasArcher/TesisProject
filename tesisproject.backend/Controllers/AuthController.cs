using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Auth.Articles;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly IArticleUserContext _articleUser;
        private readonly IAuthorizationService _authorization;
        private readonly ArticlesModuleOptions _articlesModule;

        public AuthController(
            IAuthService auth,
            IArticleUserContext articleUser,
            IAuthorizationService authorization,
            IOptions<ArticlesModuleOptions> articlesModule)
        {
            _auth = auth;
            _articleUser = articleUser;
            _authorization = authorization;
            _articlesModule = articlesModule.Value;
        }

        // ===== Cookies helpers =====
        private static void SetRefreshCookie(HttpResponse resp, string refreshToken, DateTime expiresUtc)
        {
            resp.Cookies.Append("rt", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = expiresUtc,
                IsEssential = true,
                Path = "/"
            });
        }

        private static void ClearRefreshCookie(HttpResponse resp)
        {
            resp.Cookies.Delete("rt", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            });
        }

        private static Dictionary<string, string[]> ToValidationErrors(ModelStateDictionary modelState)
        {
            return modelState
                .Where(x => x.Value is not null && x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage)
                        .ToArray()
                );
        }

        // =============== REGISTER ===============

        [HttpPost("register")]
        [ProducesResponseType(typeof(ServiceResult<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<AuthResponse>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ServiceResult<AuthResponse>>> Register(
            [FromBody] RegisterRequest dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return ServiceResult<AuthResponse>.Fail(
                    message: "Validation error.",
                    error: ErrorType.Validation,
                    errorCode: "AUTH_REGISTER_INVALID_MODEL",
                    validation: ToValidationErrors(ModelState))
                    .ToActionResult();
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (result, cookie) = await _auth.RegisterAsync(dto, ip, ct);

            if (cookie is not null)
                SetRefreshCookie(Response, cookie.Value.token, cookie.Value.exp);

            return result.ToActionResult();
        }

        // =============== LOGIN ===============

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ServiceResult<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<AuthResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceResult<AuthResponse>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ServiceResult<AuthResponse>>> Login(
            [FromBody] LoginRequest dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return ServiceResult<AuthResponse>.Fail(
                    message: "Validation error.",
                    error: ErrorType.Validation,
                    errorCode: "AUTH_LOGIN_INVALID_MODEL",
                    validation: ToValidationErrors(ModelState))
                    .ToActionResult();
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (result, cookie) = await _auth.LoginAsync(dto, ip, ct);

            if (cookie is not null)
                SetRefreshCookie(Response, cookie.Value.token, cookie.Value.exp);

            return result.ToActionResult();
        }

        // =============== REFRESH ===============

        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ServiceResult<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<AuthResponse>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ServiceResult<AuthResponse>>> Refresh(
            CancellationToken ct)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var raw = Request.Cookies["rt"];

            var (result, cookie) = await _auth.RefreshAsync(raw, ip, ct);

            if (cookie is not null)
                SetRefreshCookie(Response, cookie.Value.token, cookie.Value.exp);

            return result.ToActionResult();
        }

        // =============== CURRENT SESSION ===============
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResult<CurrentSessionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<CurrentSessionResponse>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ServiceResult<CurrentSessionResponse>>> Me(
            CancellationToken ct)
        {
            var identityUserId = _articleUser.IdentityUserId;
            if (!identityUserId.HasValue)
            {
                return ServiceResult<CurrentSessionResponse>
                    .Fail(
                        "La sesion no contiene un identificador de usuario valido.",
                        ErrorType.Unauthorized,
                        "AUTH_USER_ID_MISSING")
                    .ToActionResult();
            }

            var enabled = _articlesModule.Enabled;

            async Task<bool> CanAsync(string policy)
                => enabled && (await _authorization.AuthorizeAsync(User, policy)).Succeeded;

            var response = new CurrentSessionResponse
            {
                IdentityUserId = identityUserId.Value,
                AppUserId = await _articleUser.GetAppUserIdAsync(ct),
                Email = _articleUser.Email,
                DisplayName = _articleUser.DisplayName,
                Roles = _articleUser.Roles,
                Permissions = _articleUser.Permissions,
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
                .Ok(response, "Sesion activa.")
                .ToActionResult();
        }

        // =============== LOGOUT ===============

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResult<NoContent>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<NoContent>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ServiceResult<NoContent>>> Logout(
            CancellationToken ct)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var raw = Request.Cookies["rt"];

            var result = await _auth.RevokeAsync(raw, ip, ct);
            ClearRefreshCookie(Response);

            return result.ToActionResult();
        }
    }
}
