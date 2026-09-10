using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces;

public sealed record UnifiedAuthorWriteRequest(int? AppUserId, int? ExternalResearcherId, string? Orcid = null);
public sealed record UnifiedAuthorResponse(int AuthorId, int? AppUserId, int? ExternalResearcherId, string? Orcid);

/// <summary>Explicit CRUD for Author. Source IDs refer to business AppUser/ExternalResearcher keys.</summary>
public interface IUnifiedAuthorService
{
    Task<ServiceResult<UnifiedAuthorResponse>> GetByIdAsync(int authorId, CancellationToken ct = default);
    Task<ServiceResult<IReadOnlyList<UnifiedAuthorResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceResult<UnifiedAuthorResponse>> GetByAppUserIdAsync(int appUserId, CancellationToken ct = default);
    Task<ServiceResult<UnifiedAuthorResponse>> GetByExternalResearcherIdAsync(int externalResearcherId, CancellationToken ct = default);
    Task<ServiceResult<UnifiedAuthorResponse>> CreateAsync(UnifiedAuthorWriteRequest request, CancellationToken ct = default);
    Task<ServiceResult<UnifiedAuthorResponse>> UpdateAsync(int authorId, UnifiedAuthorWriteRequest request, CancellationToken ct = default);
    Task<ServiceResult<NoContent>> DeleteAsync(int authorId, CancellationToken ct = default);
}
