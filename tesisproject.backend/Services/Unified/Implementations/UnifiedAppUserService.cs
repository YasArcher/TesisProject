using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations;

public sealed class UnifiedAppUserService : IUnifiedAppUserService
{
    private readonly IUnifiedUnitOfWork _uow;
    private readonly IUnifiedIdentityProvisioningService _identity;
    public UnifiedAppUserService(IUnifiedUnitOfWork uow, IUnifiedIdentityProvisioningService identity)
    { _uow = uow; _identity = identity; }
    public Task<ServiceResult<int>> EnsureAppUserAsync(tesisproject.shared.DTOs.Auth.RegisterRequest request, CancellationToken ct = default)
        => _identity.EnsureAsync(request, ct);
    public Task<ServiceResult<List<int>>> EnsureAppUsersAsync(IEnumerable<tesisproject.shared.DTOs.Auth.RegisterRequest> requests, CancellationToken ct = default)
        => _identity.EnsureSelectedAsync(requests, ct);
        public async Task<ServiceResult<int>> GetAppUserIdByLocalIdAsync(
            int localUserId,
            CancellationToken ct = default)
        {
            try
            {
                if (localUserId <= 0)
                {
                    return ServiceResult<int>.Fail(
                        ErrorMessages.AppUser.InvalidLocalUserId,
                        ErrorType.Validation,
                        ErrorCodes.AppUser.InvalidLocalUserId);
                }

                var appUser = await _uow.AppUsers.GetByLocalIdAsync(localUserId, ct);

                if (appUser is null || appUser.IdUser <= 0)
                {
                    return ServiceResult<int>.Fail(
                        ErrorMessages.AppUser.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.AppUser.NotFound);
                }

                return ServiceResult<int>.Ok(appUser.IdUser, "App user resolved.");
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Fail(
                    ex.Message,
                    ErrorType.Unexpected,
                    ErrorCodes.Common.UnexpectedError);
            }
        }
}
