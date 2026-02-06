using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
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

        public AuthController(IAuthService auth) => _auth = auth;

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

        // =============== REGISTER ===============

        [HttpPost("register")]
        [Authorize(Roles = "superadmin")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(
            [FromBody] RegisterRequest dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (result, cookie) = await _auth.RegisterAsync(dto, ip, ct);

            if (cookie is not null)
                SetRefreshCookie(Response, cookie.Value.token, cookie.Value.exp);

            return result.ToActionResult();
        }

        // =============== LOGIN ===============

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
            [FromBody] LoginRequest dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (result, cookie) = await _auth.LoginAsync(dto, ip, ct);

            if (cookie is not null)
                SetRefreshCookie(Response, cookie.Value.token, cookie.Value.exp);

            return result.ToActionResult();
        }

        // =============== REFRESH ===============

        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(
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
        [Authorize] // opcional
        [ProducesResponseType(typeof(ApiResponse<NoContent>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<NoContent>>> Logout(
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