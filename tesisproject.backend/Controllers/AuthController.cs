using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Auth;

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

        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (data, status, error, cookie) = await _auth.RegisterAsync(dto, ip, ct);

            if (cookie is not null)
                SetRefreshCookie(Response, cookie.Value.token, cookie.Value.exp);

            return status switch
            {
                StatusCodes.Status200OK => Ok(data),
                StatusCodes.Status400BadRequest => BadRequest(new { error }),
                _ => StatusCode(status, new { error })
            };
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (data, status, error, cookie) = await _auth.LoginAsync(dto, ip, ct);

            if (cookie is not null)
                SetRefreshCookie(Response, cookie.Value.token, cookie.Value.exp);

            return status switch
            {
                StatusCodes.Status200OK => Ok(data),
                StatusCodes.Status401Unauthorized => Unauthorized(new { error }),
                StatusCodes.Status400BadRequest => BadRequest(new { error }),
                _ => StatusCode(status, new { error })
            };
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh(CancellationToken ct)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var raw = Request.Cookies["rt"];

            var (data, status, error, cookie) = await _auth.RefreshAsync(raw, ip, ct);

            if (cookie is not null)
                SetRefreshCookie(Response, cookie.Value.token, cookie.Value.exp);

            return status == StatusCodes.Status200OK
                ? Ok(data)
                : StatusCode(status, new { error });
        }

        [HttpPost("logout")]
        [Authorize] // opcional
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var raw = Request.Cookies["rt"];

            await _auth.RevokeAsync(raw, ip, ct);
            ClearRefreshCookie(Response);

            return NoContent();
        }
    }
}
