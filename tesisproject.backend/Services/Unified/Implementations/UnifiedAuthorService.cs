using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.UnifiedEntities.Authors;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations;

public sealed class UnifiedAuthorService : IUnifiedAuthorService
{
    private readonly IUnifiedUnitOfWork _uow;

    public UnifiedAuthorService(IUnifiedUnitOfWork uow) => _uow = uow;

    public Task<ServiceResult<UnifiedAuthorResponse>> GetByIdAsync(int authorId, CancellationToken ct = default) =>
        ReadAsync(authorId, () => _uow.Authors.GetByIdAsync(new object[] { authorId }, ct));

    public Task<ServiceResult<UnifiedAuthorResponse>> GetByAppUserIdAsync(int appUserId, CancellationToken ct = default) =>
        ReadAsync(appUserId, () => _uow.Authors.GetByAppUserIdAsync(appUserId, ct));

    public Task<ServiceResult<UnifiedAuthorResponse>> GetByExternalResearcherIdAsync(int externalResearcherId, CancellationToken ct = default) =>
        ReadAsync(externalResearcherId, () => _uow.Authors.GetByExternalResearcherIdAsync(externalResearcherId, ct));

    public Task<ServiceResult<IReadOnlyList<UnifiedAuthorResponse>>> GetAllAsync(CancellationToken ct = default) =>
        ExecuteAsync<IReadOnlyList<UnifiedAuthorResponse>>(async () =>
        {
            var authors = await _uow.Authors.GetAllAsync(ct: ct);
            return ServiceResult<IReadOnlyList<UnifiedAuthorResponse>>.Ok(authors.OrderBy(x => x.AuthorId).Select(Map).ToList());
        });

    // Public mutations own the single final save. Product synchronization must stage its
    // own aggregate through the UoW, not call these committing CRUD methods internally.
    public Task<ServiceResult<UnifiedAuthorResponse>> CreateAsync(UnifiedAuthorWriteRequest request, CancellationToken ct = default) =>
        ExecuteAsync(async () =>
        {
            var failure = await ValidateAsync(request, null, ct);
            if (failure is not null) return failure;
            var author = new Author
            {
                AppUserId = request.AppUserId,
                ExternalResearcherId = request.ExternalResearcherId,
                Orcid = NormalizeOrcid(request.Orcid)
            };
            await _uow.Authors.AddAsync(author, ct);
            await _uow.SaveChangesAsync(ct);
            return ServiceResult<UnifiedAuthorResponse>.Ok(Map(author));
        });

    public Task<ServiceResult<UnifiedAuthorResponse>> UpdateAsync(int authorId, UnifiedAuthorWriteRequest request, CancellationToken ct = default) =>
        ExecuteAsync(async () =>
        {
            if (authorId <= 0) return InvalidId<UnifiedAuthorResponse>();
            var author = await _uow.Authors.GetByIdAsync(new object[] { authorId }, ct);
            if (author is null) return NotFound<UnifiedAuthorResponse>();
            var failure = await ValidateAsync(request, authorId, ct);
            if (failure is not null) return failure;
            author.AppUserId = request.AppUserId;
            author.ExternalResearcherId = request.ExternalResearcherId;
            author.Orcid = NormalizeOrcid(request.Orcid);
            // ExternalAuthorId remains untouched: legacy Articles compatibility only.
            _uow.Authors.Update(author);
            await _uow.SaveChangesAsync(ct);
            return ServiceResult<UnifiedAuthorResponse>.Ok(Map(author));
        });

    public Task<ServiceResult<NoContent>> DeleteAsync(int authorId, CancellationToken ct = default) =>
        ExecuteAsync(async () =>
        {
            if (authorId <= 0) return InvalidId<NoContent>();
            var author = await _uow.Authors.GetByIdAsync(new object[] { authorId }, ct);
            if (author is null) return NotFound<NoContent>();
            if (await _uow.ProductAuthors.ExistsAsync(x => x.AuthorId == authorId, ct))
                return ServiceResult<NoContent>.Fail(ErrorMessages.Author.InUse, ErrorType.Conflict, ErrorCodes.Author.InUse);
            _uow.Authors.Remove(author);
            await _uow.SaveChangesAsync(ct);
            return ServiceResult<NoContent>.Ok(new NoContent());
        });

    private async Task<ServiceResult<UnifiedAuthorResponse>?> ValidateAsync(
        UnifiedAuthorWriteRequest? request, int? excludingAuthorId, CancellationToken ct)
    {
        if (request is null)
            return Validation(ErrorMessages.Common.RequestRequired, ErrorCodes.Common.RequestRequired, "Request");
        if (request.AppUserId.HasValue == request.ExternalResearcherId.HasValue)
            return Validation(ErrorMessages.Author.ExactlyOneSource, ErrorCodes.Author.ExactlyOneSource,
                nameof(request.AppUserId), nameof(request.ExternalResearcherId));
        if (request.AppUserId is <= 0 || request.ExternalResearcherId is <= 0)
            return Validation(ErrorMessages.Common.InvalidId, ErrorCodes.Common.InvalidId,
                request.AppUserId.HasValue ? nameof(request.AppUserId) : nameof(request.ExternalResearcherId));
        var orcid = NormalizeOrcid(request.Orcid);
        if (orcid?.Length > 50)
            return Validation(ErrorMessages.Author.OrcidTooLong, ErrorCodes.Author.OrcidTooLong, nameof(request.Orcid));

        Author? existing;
        if (request.AppUserId is int appUserId)
        {
            if (await _uow.AppUsers.GetByIdAsync(new object[] { appUserId }, ct) is null)
                return ServiceResult<UnifiedAuthorResponse>.Fail(ErrorMessages.AppUser.NotFound, ErrorType.NotFound, ErrorCodes.AppUser.NotFound);
            existing = await _uow.Authors.GetByAppUserIdAsync(appUserId, ct);
        }
        else
        {
            var externalResearcherId = request.ExternalResearcherId!.Value;
            if (await _uow.ExternalResearchers.GetByIdAsync(new object[] { externalResearcherId }, ct) is null)
                return ServiceResult<UnifiedAuthorResponse>.Fail(ErrorMessages.ExternalResearcher.NotFound, ErrorType.NotFound, ErrorCodes.ExternalResearcher.NotFound);
            existing = await _uow.Authors.GetByExternalResearcherIdAsync(externalResearcherId, ct);
        }
        if (existing is not null && existing.AuthorId != excludingAuthorId)
            return ServiceResult<UnifiedAuthorResponse>.Fail(ErrorMessages.Author.SourceAlreadyAssigned, ErrorType.Conflict, ErrorCodes.Author.SourceAlreadyAssigned);
        if (orcid is not null && await _uow.Authors.ExistsAsync(x => x.Orcid == orcid && x.AuthorId != excludingAuthorId, ct))
            return ServiceResult<UnifiedAuthorResponse>.Fail(ErrorMessages.Author.OrcidAlreadyExists, ErrorType.Conflict, ErrorCodes.Author.OrcidAlreadyExists);
        return null; // Internal validation sentinel, never an operational success/placeholder.
    }

    private static Task<ServiceResult<UnifiedAuthorResponse>> ReadAsync(int id, Func<Task<Author?>> read) =>
        ExecuteAsync(async () =>
        {
            if (id <= 0) return InvalidId<UnifiedAuthorResponse>();
            var author = await read();
            return author is null ? NotFound<UnifiedAuthorResponse>() : ServiceResult<UnifiedAuthorResponse>.Ok(Map(author));
        });

    private static UnifiedAuthorResponse Map(Author author) => new(author.AuthorId, author.AppUserId, author.ExternalResearcherId, author.Orcid);
    private static string? NormalizeOrcid(string? orcid) => string.IsNullOrWhiteSpace(orcid) ? null : orcid.Trim();
    private static ServiceResult<T> InvalidId<T>() => ServiceResult<T>.Fail(ErrorMessages.Common.InvalidId, ErrorType.Validation, ErrorCodes.Common.InvalidId);
    private static ServiceResult<T> NotFound<T>() => ServiceResult<T>.Fail(ErrorMessages.Author.NotFound, ErrorType.NotFound, ErrorCodes.Author.NotFound);
    private static ServiceResult<UnifiedAuthorResponse> Validation(string message, string code, params string[] fields) =>
        ServiceResult<UnifiedAuthorResponse>.Fail(message, ErrorType.Validation, code, fields.ToDictionary(x => x, _ => new[] { message }));

    private static async Task<ServiceResult<T>> ExecuteAsync<T>(Func<Task<ServiceResult<T>>> action)
    {
        try { return await action(); }
        catch (OperationCanceledException)
        {
            return ServiceResult<T>.Fail(ErrorMessages.Common.OperationCanceled, ErrorType.Unexpected, ErrorCodes.Common.OperationCanceled);
        }
        catch (DbUpdateException)
        {
            return ServiceResult<T>.Fail(ErrorMessages.Common.PersistenceConflict, ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);
        }
        catch (Exception)
        {
            return ServiceResult<T>.Fail(ErrorMessages.Common.UnexpectedError, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
        }
    }
}
