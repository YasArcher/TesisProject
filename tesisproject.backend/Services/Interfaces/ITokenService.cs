using tesisproject.backend.Data.Identity;

namespace tesisproject.backend.Services.Interfaces
{
    public interface ITokenService
    {
        (string token, DateTime expiresAtUtc) CreateAccessToken(ApplicationUser user, IList<string> roles);
    }
}
