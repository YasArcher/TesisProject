using System.Security.Claims;
using tesisproject.shared.DTOs.Workflow;

namespace tesisproject.backend.Services.Interfaces;

public interface IRegistrationWorkflowSettingsService
{
    Task<RegistrationWorkflowSettingsDto> GetAsync(CancellationToken ct = default);
    Task<RegistrationWorkflowSettingsDto> UpdateAsync(UpdateRegistrationWorkflowSettingsRequest request, ClaimsPrincipal user, CancellationToken ct = default);
    Task<bool> CanInitiateRegistrationAsync(ClaimsPrincipal user, CancellationToken ct = default);
    Task<bool> CanInitiateArticleRegistrationAsync(ClaimsPrincipal user, CancellationToken ct = default);
    Task<bool> CanInitiateMatrixRegistrationAsync(ClaimsPrincipal user, CancellationToken ct = default);
}
