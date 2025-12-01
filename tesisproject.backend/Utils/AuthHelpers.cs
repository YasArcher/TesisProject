using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace tesisproject.backend.Utils
{
    public static class AuthHelpers
    {
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            if (user is null) return null;

            // 1) Intentar con ClaimTypes.NameIdentifier (el de Identity)
            var claim = user.FindFirst(ClaimTypes.NameIdentifier);

            // 2) Si no está, intentar con "sub"
            if (claim == null)
                claim = user.FindFirst(JwtRegisteredClaimNames.Sub);

            if (claim == null)
                return null;

            return int.TryParse(claim.Value, out var id)
                ? id
                : (int?)null;
        }
    }
}
