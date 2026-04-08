using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Identity;
using tesisproject.shared.Abstractions.Auth;
using tesisproject.shared.DTOs.Auth;

namespace tesisproject.backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [Authorize(Policy = AppPolicies.SecurityAdministration)]
    [HttpPost("register")]
    public async Task<ActionResult<LoguinResponse>> Register(RegisterUserRequest dto)
        => Ok(await _auth.RegisterAsync(dto));

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoguinResponse>> Login(LoguinRequest dto)
        => Ok(await _auth.LoginAsync(dto));

    [Authorize(Policy = AppPolicies.AuthenticatedUser)]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        try
        {
            var (email, fullName, roles) = await _auth.MeAsync(User);
            return Ok(new { email, fullName, roles });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "La sesión actual no es válida o no está autenticada." });
        }
    }
}
