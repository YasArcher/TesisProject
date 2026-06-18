namespace tesisproject.frontend.Services.Interfaces;

public interface ITokenStore
{
    Task SetAsync(string token);
    Task<string?> GetAsync();
    Task ClearAsync();
}