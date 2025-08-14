namespace tesisproject.frontend.Services.Interfaces
{
    public interface ITokenStore
    {
        ValueTask SetAsync(string token);
        ValueTask<string?> GetAsync();
        ValueTask ClearAsync();
    }
}