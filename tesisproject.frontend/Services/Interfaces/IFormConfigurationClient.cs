using tesisproject.shared.DTOs.Catalogs;
using tesisproject.shared.DTOs.Configuration;

namespace tesisproject.frontend.Services.Interfaces;

public interface IFormConfigurationClient
{
    Task<HttpResponseWrapper<List<FormSummaryDto>?>> GetFormsAsync(string? entityName = null, CancellationToken ct = default);
    Task<HttpResponseWrapper<List<FormFieldAdminDto>?>> GetFormFieldsAsync(int formId, CancellationToken ct = default);
    Task<HttpResponseWrapper<List<FieldCatalogItemDto>?>> GetFieldsAsync(string entityName, CancellationToken ct = default);
    Task<HttpResponseWrapper<List<FieldCatalogItemDto>?>> GetDynamicFieldsAsync(string entityName, CancellationToken ct = default);
    Task<HttpResponseWrapper<ResolvedFormDto?>> GetResolvedFormAsync(string formKey, CancellationToken ct = default);
    Task<HttpResponseWrapper<ResolvedFormDto?>> GetActiveResolvedFormAsync(string entityName, string? preferredFormKey = null, CancellationToken ct = default);
    Task<HttpResponseWrapper<List<CatalogItemDto>?>> GetCatalogItemsByFieldAsync(int fieldId, int? parentId = null, CancellationToken ct = default);
    Task<HttpResponseWrapper<FormDefinitionAdminDto?>> CreateFormAsync(CreateFormDefinitionRequest request, CancellationToken ct = default);
    Task<HttpResponseWrapper<FormDefinitionAdminDto?>> UpdateFormAsync(int formId, UpdateFormDefinitionRequest request, CancellationToken ct = default);
    Task<HttpResponseWrapper<tesisproject.shared.Responses.NoContent?>> DeleteFormAsync(int formId, CancellationToken ct = default);
    Task<HttpResponseWrapper<FieldCatalogItemDto?>> CreateDynamicFieldAsync(CreateDynamicFieldRequest request, CancellationToken ct = default);
    Task<HttpResponseWrapper<FieldCatalogItemDto?>> UpdateFieldAsync(int fieldId, UpdateFieldCatalogRequest request, CancellationToken ct = default);
    Task<HttpResponseWrapper<tesisproject.shared.Responses.NoContent?>> DeleteFieldAsync(int fieldId, CancellationToken ct = default);
    Task<HttpResponseWrapper<FormFieldAdminDto?>> AddFieldToFormAsync(int formId, AddFieldToFormRequest request, CancellationToken ct = default);
    Task<HttpResponseWrapper<FormFieldAdminDto?>> UpdateFormFieldAsync(int formId, int formFieldId, UpdateFormFieldRequest request, CancellationToken ct = default);
    Task<HttpResponseWrapper<List<CatalogAdminItemDto>?>> GetAdminCatalogAsync(string catalogKey, CancellationToken ct = default);
    Task<HttpResponseWrapper<CatalogAdminItemDto?>> CreateAdminCatalogItemAsync(string catalogKey, UpsertCatalogItemRequest request, CancellationToken ct = default);
    Task<HttpResponseWrapper<CatalogAdminItemDto?>> UpdateAdminCatalogItemAsync(string catalogKey, int id, UpsertCatalogItemRequest request, CancellationToken ct = default);
    Task<HttpResponseWrapper<tesisproject.shared.Responses.NoContent?>> DeleteAdminCatalogItemAsync(string catalogKey, int id, CancellationToken ct = default);
    Task<HttpResponseWrapper<List<DynamicFieldOptionDto>?>> GetFieldOptionsAsync(int fieldId, CancellationToken ct = default);
    Task<HttpResponseWrapper<DynamicFieldOptionDto?>> CreateFieldOptionAsync(int fieldId, CreateDynamicFieldOptionRequest request, CancellationToken ct = default);
    Task<HttpResponseWrapper<DynamicFieldOptionDto?>> UpdateFieldOptionAsync(int fieldId, int optionId, UpdateDynamicFieldOptionRequest request, CancellationToken ct = default);
    Task<HttpResponseWrapper<tesisproject.shared.Responses.NoContent?>> DeleteFieldOptionAsync(int fieldId, int optionId, CancellationToken ct = default);
    Task<HttpResponseWrapper<tesisproject.shared.Responses.NoContent?>> DeleteFormFieldAsync(int formId, int formFieldId, CancellationToken ct = default);
}
