using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace tesisproject.backend.Utils
{
    public static class AuthHelpers
    {
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            if (user is null) return null;

            var claim = user.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
                claim = user.FindFirst(JwtRegisteredClaimNames.Sub);

            if (claim == null)
                return null;

            return int.TryParse(claim.Value, out var id)
                ? id
                : (int?)null;
        }

        public static int GetRequiredUserId(this ClaimsPrincipal user)
        {
            var userId = user.GetUserId();

            if (!userId.HasValue)
                throw new UnauthorizedAccessException("AUTH_USER_NOT_AUTHENTICATED");

            return userId.Value;
        }
    }
}