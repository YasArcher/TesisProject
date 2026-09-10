using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class UnifiedAuthController : ControllerBase
    {
        private readonly IUnifiedAuthService _auth;
        private readonly IUnifiedCurrentSessionService _session;
        public UnifiedAuthController(IUnifiedAuthService auth, IUnifiedCurrentSessionService session)
        { _auth = auth; _session = session; }

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResult<CurrentSessionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResult<CurrentSessionResponse>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ServiceResult<CurrentSessionResponse>>> Me(CancellationToken ct)
            => (await _session.GetAsync(User, ct)).ToActionResult();

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
