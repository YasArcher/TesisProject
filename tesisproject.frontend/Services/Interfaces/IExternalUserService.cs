using tesisproject.shared.DTOs.AppUser;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IExternalUserService
    {
        /// <summary>Get a list of external users.</summary>
        Task<HttpResponseWrapper<List<ResolvedUserProfileDTO>?>> GetListAsync(CancellationToken ct = default);

    }
}
