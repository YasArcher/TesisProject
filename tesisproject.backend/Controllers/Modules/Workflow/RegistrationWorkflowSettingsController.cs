using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Workflow;

namespace tesisproject.backend.Controllers.Modules.Workflow;

[ApiController]
[Route("api/registration-workflow-settings")]
[Authorize(Policy = AppPolicies.AuthenticatedUser)]
public sealed class RegistrationWorkflowSettingsController : ControllerBase
{
    private readonly IRegistrationWorkflowSettingsService _settings;

    public RegistrationWorkflowSettingsController(IRegistrationWorkflowSettingsService settings)
    {
        _settings = settings;
    }

    [HttpGet]
    public Task<RegistrationWorkflowSettingsDto> Get(CancellationToken ct)
        => _settings.GetAsync(ct);

    [HttpPut]
    [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
    public Task<RegistrationWorkflowSettingsDto> Update(
        [FromBody] UpdateRegistrationWorkflowSettingsRequest request,
        CancellationToken ct)
        => _settings.UpdateAsync(request, User, ct);
}
