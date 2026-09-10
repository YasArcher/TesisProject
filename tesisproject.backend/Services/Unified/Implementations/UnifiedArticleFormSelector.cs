using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;

namespace tesisproject.backend.Services.Unified.Implementations;

public sealed class UnifiedArticleFormSelector(IUnifiedArticleRegistrationRepository repository) : IUnifiedArticleFormSelector
{
    public async Task<FormDefinition?> SelectAsync(string entityName, string? formKey, CancellationToken ct = default)
    {
        var forms = await repository.GetActiveFormsAsync(entityName.Trim(), ct);
        var key = formKey?.Trim();
        var normalized = Normalize(key);
        return forms.OrderBy(f => !string.IsNullOrEmpty(key) && string.Equals(f.FormKey.Trim(), key, StringComparison.OrdinalIgnoreCase) ? 0
                : normalized.Length > 0 && Normalize(f.FormKey) == normalized ? 1 : 2)
            .ThenByDescending(f => f.UpdatedAt ?? f.CreatedAt).ThenByDescending(f => f.FormId).FirstOrDefault();
    }
    internal static string Normalize(string? value) => new((value ?? "").Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
