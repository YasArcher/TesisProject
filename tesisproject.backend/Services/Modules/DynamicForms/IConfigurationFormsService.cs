using tesisproject.shared.DTOs.Configuration;
using tesisproject.shared.DTOs.Catalogs;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IConfigurationFormsService
    {
        Task<List<FormSummaryDto>> GetFormsAsync(string? entityName = null, CancellationToken ct = default);
        Task<List<FormFieldAdminDto>> GetFormFieldsAsync(int formId, CancellationToken ct = default);
        Task<List<FieldCatalogItemDto>> GetFieldsByEntityAsync(string entityName, CancellationToken ct = default);
        Task<List<FieldCatalogItemDto>> GetDynamicFieldsByEntityAsync(string entityName, CancellationToken ct = default);
        Task<List<DynamicFieldOptionDto>> GetFieldOptionsAsync(int fieldId, CancellationToken ct = default);
        Task<DynamicFieldOptionDto> CreateFieldOptionAsync(int fieldId, CreateDynamicFieldOptionRequest request, CancellationToken ct = default);
        Task<DynamicFieldOptionDto?> UpdateFieldOptionAsync(int fieldId, int optionId, UpdateDynamicFieldOptionRequest request, CancellationToken ct = default);
        Task<List<CatalogItemDto>> GetCatalogItemsByFieldAsync(int fieldId, int? parentId = null, CancellationToken ct = default);
        Task<ResolvedFormDto?> GetResolvedFormAsync(string formKey, CancellationToken ct = default);
        Task<ResolvedFormDto?> GetActiveResolvedFormAsync(string entityName, string? preferredFormKey = null, CancellationToken ct = default);
        Task<FormDefinitionAdminDto> CreateFormAsync(CreateFormDefinitionRequest request, CancellationToken ct = default);
        Task<FormDefinitionAdminDto?> UpdateFormAsync(int formId, UpdateFormDefinitionRequest request, CancellationToken ct = default);
        Task<bool> DeleteFormAsync(int formId, CancellationToken ct = default);
        Task<FieldCatalogItemDto> CreateDynamicFieldAsync(CreateDynamicFieldRequest request, CancellationToken ct = default);
        Task<FieldCatalogItemDto?> UpdateFieldAsync(int fieldId, UpdateFieldCatalogRequest request, CancellationToken ct = default);
        Task<bool> DeleteFieldAsync(int fieldId, CancellationToken ct = default);
        Task<FormFieldAdminDto> AddFieldToFormAsync(int formId, AddFieldToFormRequest request, CancellationToken ct = default);
        Task<FormFieldAdminDto?> UpdateFormFieldAsync(int formId, int formFieldId, UpdateFormFieldRequest request, CancellationToken ct = default);
        Task<bool> RemoveFormFieldAsync(int formId, int formFieldId, CancellationToken ct = default);
        Task<bool> DeleteFieldOptionAsync(int fieldId, int optionId, CancellationToken ct = default);
    }
}
