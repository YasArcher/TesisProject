using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Workflow;

namespace tesisproject.frontend.Services.Implementations;

public sealed class RegistrationWorkflowSettingsClient : IRegistrationWorkflowSettingsClient
{
    private readonly IApiClient _api;

    public RegistrationWorkflowSettingsClient(IApiClient api)
    {
        _api = api;
    }

    public Task<RegistrationWorkflowSettingsDto?> GetAsync(CancellationToken ct = default)
        => _api.GetAsync<RegistrationWorkflowSettingsDto>("api/registration-workflow-settings", ct);

    public Task<RegistrationWorkflowSettingsDto?> UpdateAsync(UpdateRegistrationWorkflowSettingsRequest request, CancellationToken ct = default)
        => _api.PutAsync<UpdateRegistrationWorkflowSettingsRequest, RegistrationWorkflowSettingsDto>("api/registration-workflow-settings", request, ct);
}
