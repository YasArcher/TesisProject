using tesisproject.backend.Data.UnifiedEntities.Articles;

namespace tesisproject.backend.Services.Unified.Interfaces;

public interface IUnifiedArticleFormSelector
{
    Task<FormDefinition?> SelectAsync(string entityName, string? formKey, CancellationToken ct = default);
}
