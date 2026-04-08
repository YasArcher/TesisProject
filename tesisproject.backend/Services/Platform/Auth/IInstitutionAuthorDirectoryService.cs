namespace tesisproject.backend.Services.Implementations;

public interface IInstitutionAuthorDirectoryService
{
    Task<InstitutionAuthorEligibilityResult> ValidateAuthorAsync(string email, CancellationToken ct = default);
}

public sealed record InstitutionAuthorEligibilityResult(
    bool IsAllowed,
    string? FullName = null,
    string? SourceReference = null,
    string? Reason = null);
