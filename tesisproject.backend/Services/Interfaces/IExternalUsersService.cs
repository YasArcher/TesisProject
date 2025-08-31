namespace tesisproject.backend.Services.Interfaces
{
    public interface IExternalUsersService
    {
        /// <summary>Checks whether an external user exists in the external Users API.</summary>
        Task<bool> UserExistsAsync(int externalUserId, CancellationToken ct = default);
    }
}
