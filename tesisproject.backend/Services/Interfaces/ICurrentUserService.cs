namespace tesisproject.backend.Services.Interfaces
{
    public interface ICurrentUserService
    {
        int? GetUserId();
        int GetRequiredUserId();
    }
}