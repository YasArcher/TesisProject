using tesisproject.shared.DTOs.Workflow;

namespace tesisproject.frontend.Services.Interfaces;

public interface IRegistrationWorkflowSettingsClient
{
    Task<RegistrationWorkflowSettingsDto?> GetAsync(CancellationToken ct = default);
    Task<RegistrationWorkflowSettingsDto?> UpdateAsync(UpdateRegistrationWorkflowSettingsRequest request, CancellationToken ct = default);
}
