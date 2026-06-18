using Microsoft.Extensions.Options;
using tesisproject.backend.Options;

namespace tesisproject.backend.Services.Implementations;

public class LocalInstitutionAuthorDirectoryService : IInstitutionAuthorDirectoryService
{
    private readonly InstitutionIdentityOptions _options;

    public LocalInstitutionAuthorDirectoryService(IOptions<InstitutionIdentityOptions> options)
    {
        _options = options.Value;
    }

    public Task<InstitutionAuthorEligibilityResult> ValidateAuthorAsync(string email, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_options.Enabled)
        {
            return Task.FromResult(new InstitutionAuthorEligibilityResult(
                IsAllowed: true,
                FullName: null,
                SourceReference: "local",
                Reason: "Integración institucional no configurada."));
        }

        return Task.FromResult(new InstitutionAuthorEligibilityResult(
            IsAllowed: false,
            FullName: null,
            SourceReference: _options.SourceType,
            Reason: "La integración institucional está habilitada, pero todavía no existe un adaptador concreto para consultar docentes/autores."));
    }
}
