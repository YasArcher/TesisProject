using tesisproject.shared.DTOs.External;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IExternalUserService
    {
        /// <summary>Get a list of external users.</summary>
        Task<HttpResponseWrapper<List<ExternalUserDTO>?>> GetListAsync(CancellationToken ct = default);

    }
}
