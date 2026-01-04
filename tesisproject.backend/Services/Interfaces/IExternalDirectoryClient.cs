using tesisproject.shared.Entities.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    /// <summary>
    /// Thin client for the external directory API.
    /// It supports batch queries by emails or documents (even if only one is provided).
    /// </summary>
    public interface IExternalDirectoryClient
    {
        /// <summary>
        /// Retrieves external profiles by one or more institutional emails.
        /// The endpoint supports both single and comma-separated lists.
        /// </summary>
        Task<ServiceResult<IReadOnlyList<ExternalUserProfileModel>>> GetByEmailsAsync(IEnumerable<string> emails, CancellationToken ct = default);

        /// <summary>
        /// Retrieves external profiles by one or more documents (cedulas).
        /// The endpoint supports both single and comma-separated lists.
        /// </summary>
        Task<ServiceResult<IReadOnlyList<ExternalUserProfileModel>>> GetByDocumentsAsync(IEnumerable<string> documents, CancellationToken ct = default);

        /// <summary>
        /// Retrieves all external profiles from the directory (if supported).
        /// </summary>
        Task<ServiceResult<IReadOnlyList<ExternalUserProfileModel>>> GetAllAsync(CancellationToken ct = default);
    }
}
