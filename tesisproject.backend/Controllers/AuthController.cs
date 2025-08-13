using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Data.Identity;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Auth;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _token;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService token)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _token = token;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest dto)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                UserName = dto.Email,
                FullName = dto.FullName
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Opcional: asignar rol por defecto
            // await _userManager.AddToRoleAsync(user, "User");

            var roles = await _userManager.GetRolesAsync(user);
            var (token, exp) = _token.CreateAccessToken(user, roles);
            return Ok(new AuthResponse { AccessToken = token, ExpiresAtUtc = exp });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null) return Unauthorized();

            var check = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
            if (!check.Succeeded) return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var (token, exp) = _token.CreateAccessToken(user, roles);
            return Ok(new AuthResponse { AccessToken = token, ExpiresAtUtc = exp });
        }
    }
}