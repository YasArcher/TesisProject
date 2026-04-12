using tesisproject.shared.DTOs.Catalogs;
using tesisproject.shared.DTOs.Configuration;
using tesisproject.frontend.Services.Platform.Api;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IFormConfigurationClient
    {
        Task<List<FormSummaryDto>> GetFormsAsync(string? entityName = null, CancellationToken ct = default);
        Task<List<FormFieldAdminDto>> GetFormFieldsAsync(int formId, CancellationToken ct = default);
        Task<List<FieldCatalogItemDto>> GetFieldsAsync(string entityName, CancellationToken ct = default);
        Task<List<FieldCatalogItemDto>> GetDynamicFieldsAsync(string entityName, CancellationToken ct = default);
        Task<ResolvedFormDto?> GetResolvedFormAsync(string formKey, CancellationToken ct = default);
        Task<ResolvedFormDto?> GetActiveResolvedFormAsync(string entityName, string? preferredFormKey = null, CancellationToken ct = default);
        Task<List<CatalogItemDto>> GetCatalogItemsByFieldAsync(int fieldId, int? parentId = null, CancellationToken ct = default);
        Task<FormDefinitionAdminDto?> CreateFormAsync(CreateFormDefinitionRequest request, CancellationToken ct = default);
        Task<FormDefinitionAdminDto?> UpdateFormAsync(int formId, UpdateFormDefinitionRequest request, CancellationToken ct = default);
        Task DeleteFormAsync(int formId, CancellationToken ct = default);
        Task<FieldCatalogItemDto?> CreateDynamicFieldAsync(CreateDynamicFieldRequest request, CancellationToken ct = default);
        Task<FieldCatalogItemDto?> UpdateFieldAsync(int fieldId, UpdateFieldCatalogRequest request, CancellationToken ct = default);
        Task<FormFieldAdminDto?> AddFieldToFormAsync(int formId, AddFieldToFormRequest request, CancellationToken ct = default);
        Task<FormFieldAdminDto?> UpdateFormFieldAsync(int formId, int formFieldId, UpdateFormFieldRequest request, CancellationToken ct = default);
        Task<List<CatalogAdminItemDto>> GetAdminCatalogAsync(string catalogKey, CancellationToken ct = default);
        Task<CatalogAdminItemDto?> CreateAdminCatalogItemAsync(string catalogKey, UpsertCatalogItemRequest request, CancellationToken ct = default);
        Task<CatalogAdminItemDto?> UpdateAdminCatalogItemAsync(string catalogKey, int id, UpsertCatalogItemRequest request, CancellationToken ct = default);
        Task<List<DynamicFieldOptionDto>> GetFieldOptionsAsync(int fieldId, CancellationToken ct = default);
        Task<DynamicFieldOptionDto?> CreateFieldOptionAsync(int fieldId, CreateDynamicFieldOptionRequest request, CancellationToken ct = default);
        Task<DynamicFieldOptionDto?> UpdateFieldOptionAsync(int fieldId, int optionId, UpdateDynamicFieldOptionRequest request, CancellationToken ct = default);
        Task DeleteFieldOptionAsync(int fieldId, int optionId, CancellationToken ct = default);
        Task DeleteFormFieldAsync(int formId, int formFieldId, CancellationToken ct = default);

        Task<HttpResponseWrapper<List<FormSummaryDto>?>> GetFormsResultAsync(string? entityName = null, CancellationToken ct = default);
        Task<HttpResponseWrapper<List<FormFieldAdminDto>?>> GetFormFieldsResultAsync(int formId, CancellationToken ct = default);
        Task<HttpResponseWrapper<List<FieldCatalogItemDto>?>> GetFieldsResultAsync(string entityName, CancellationToken ct = default);
        Task<HttpResponseWrapper<List<FieldCatalogItemDto>?>> GetDynamicFieldsResultAsync(string entityName, CancellationToken ct = default);
        Task<HttpResponseWrapper<List<CatalogItemDto>?>> GetCatalogItemsByFieldResultAsync(int fieldId, int? parentId = null, CancellationToken ct = default);
        Task<HttpResponseWrapper<List<DynamicFieldOptionDto>?>> GetFieldOptionsResultAsync(int fieldId, CancellationToken ct = default);
        Task<HttpResponseWrapper<ResolvedFormDto?>> GetResolvedFormResultAsync(string formKey, CancellationToken ct = default);
        Task<HttpResponseWrapper<ResolvedFormDto?>> GetActiveResolvedFormResultAsync(string entityName, string? preferredFormKey = null, CancellationToken ct = default);
        Task<HttpResponseWrapper<FormDefinitionAdminDto?>> CreateFormResultAsync(CreateFormDefinitionRequest request, CancellationToken ct = default);
        Task<HttpResponseWrapper<FormDefinitionAdminDto?>> UpdateFormResultAsync(int formId, UpdateFormDefinitionRequest request, CancellationToken ct = default);
        Task<HttpResponseWrapper<object?>> DeleteFormResultAsync(int formId, CancellationToken ct = default);
        Task<HttpResponseWrapper<FieldCatalogItemDto?>> CreateDynamicFieldResultAsync(CreateDynamicFieldRequest request, CancellationToken ct = default);
        Task<HttpResponseWrapper<FieldCatalogItemDto?>> UpdateFieldResultAsync(int fieldId, UpdateFieldCatalogRequest request, CancellationToken ct = default);
        Task<HttpResponseWrapper<FormFieldAdminDto?>> AddFieldToFormResultAsync(int formId, AddFieldToFormRequest request, CancellationToken ct = default);
        Task<HttpResponseWrapper<FormFieldAdminDto?>> UpdateFormFieldResultAsync(int formId, int formFieldId, UpdateFormFieldRequest request, CancellationToken ct = default);
        Task<HttpResponseWrapper<object?>> DeleteFormFieldResultAsync(int formId, int formFieldId, CancellationToken ct = default);
        Task<HttpResponseWrapper<DynamicFieldOptionDto?>> CreateFieldOptionResultAsync(int fieldId, CreateDynamicFieldOptionRequest request, CancellationToken ct = default);
        Task<HttpResponseWrapper<DynamicFieldOptionDto?>> UpdateFieldOptionResultAsync(int fieldId, int optionId, UpdateDynamicFieldOptionRequest request, CancellationToken ct = default);
        Task<HttpResponseWrapper<object?>> DeleteFieldOptionResultAsync(int fieldId, int optionId, CancellationToken ct = default);
    }
}
