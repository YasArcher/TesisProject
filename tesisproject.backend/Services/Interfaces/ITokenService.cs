namespace tesisproject.backend.Services.Interfaces
{
    public interface ITokenService
    {
        (string token, DateTime expiresAtUtc) CreateAccessToken(int userId, string? email, IList<string> roles);
        (string token, DateTime expiresAtUtc) CreateRefreshToken();
    }
}
