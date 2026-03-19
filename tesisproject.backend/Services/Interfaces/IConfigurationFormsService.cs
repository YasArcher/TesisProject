using tesisproject.shared.DTOs.Configuration;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IConfigurationFormsService
    {
        Task<List<FormSummaryDto>> GetFormsAsync(string? entityName = null, CancellationToken ct = default);
        Task<List<FieldCatalogItemDto>> GetFieldsByEntityAsync(string entityName, CancellationToken ct = default);
        Task<List<FieldCatalogItemDto>> GetDynamicFieldsByEntityAsync(string entityName, CancellationToken ct = default);
        Task<List<DynamicFieldOptionDto>> GetFieldOptionsAsync(int fieldId, CancellationToken ct = default);
        Task<ResolvedFormDto?> GetResolvedFormAsync(string formKey, CancellationToken ct = default);
    }
}
