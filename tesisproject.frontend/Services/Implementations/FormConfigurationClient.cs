using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalogs;
using tesisproject.shared.DTOs.Configuration;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations;

public sealed class FormConfigurationClient : IFormConfigurationClient
{
    private readonly IApiClient _api;

    public FormConfigurationClient(IApiClient api)
    {
        _api = api;
    }

    public Task<HttpResponseWrapper<List<FormSummaryDto>?>> GetFormsAsync(string? entityName = null, CancellationToken ct = default)
    {
        var url = $"config/forms?cb={GetCacheBust()}";
        if (!string.IsNullOrWhiteSpace(entityName))
            url += $"&entityName={Uri.EscapeDataString(entityName)}";

        return _api.GetAsync<List<FormSummaryDto>>(url, ct);
    }

    public Task<HttpResponseWrapper<List<FormFieldAdminDto>?>> GetFormFieldsAsync(int formId, CancellationToken ct = default)
        => _api.GetAsync<List<FormFieldAdminDto>>($"config/forms/{formId}/fields?cb={GetCacheBust()}", ct);

    public Task<HttpResponseWrapper<List<FieldCatalogItemDto>?>> GetFieldsAsync(string entityName, CancellationToken ct = default)
        => _api.GetAsync<List<FieldCatalogItemDto>>($"config/fields?entityName={Uri.EscapeDataString(entityName)}&cb={GetCacheBust()}", ct);

    public Task<HttpResponseWrapper<List<FieldCatalogItemDto>?>> GetDynamicFieldsAsync(string entityName, CancellationToken ct = default)
        => _api.GetAsync<List<FieldCatalogItemDto>>($"config/fields/dynamic?entityName={Uri.EscapeDataString(entityName)}&cb={GetCacheBust()}", ct);

    public Task<HttpResponseWrapper<ResolvedFormDto?>> GetResolvedFormAsync(string formKey, CancellationToken ct = default)
        => _api.GetAsync<ResolvedFormDto>($"config/forms/{Uri.EscapeDataString(formKey)}/resolved?cb={GetCacheBust()}", ct);

    public Task<HttpResponseWrapper<ResolvedFormDto?>> GetActiveResolvedFormAsync(string entityName, string? preferredFormKey = null, CancellationToken ct = default)
    {
        var url = $"config/forms/resolved-active?entityName={Uri.EscapeDataString(entityName)}&cb={GetCacheBust()}";
        if (!string.IsNullOrWhiteSpace(preferredFormKey))
            url += $"&preferredFormKey={Uri.EscapeDataString(preferredFormKey)}";

        return _api.GetAsync<ResolvedFormDto>(url, ct);
    }

    public Task<HttpResponseWrapper<List<CatalogItemDto>?>> GetCatalogItemsByFieldAsync(int fieldId, int? parentId = null, CancellationToken ct = default)
    {
        var url = $"config/fields/{fieldId}/catalog-items?cb={GetCacheBust()}";
        if (parentId.HasValue)
            url += $"&parentId={parentId.Value}";

        return _api.GetAsync<List<CatalogItemDto>>(url, ct);
    }

    public Task<HttpResponseWrapper<FormDefinitionAdminDto?>> CreateFormAsync(CreateFormDefinitionRequest request, CancellationToken ct = default)
        => _api.PostAsync<CreateFormDefinitionRequest, FormDefinitionAdminDto>("config/forms", request, ct);

    public Task<HttpResponseWrapper<FormDefinitionAdminDto?>> UpdateFormAsync(int formId, UpdateFormDefinitionRequest request, CancellationToken ct = default)
        => _api.PutAsync<UpdateFormDefinitionRequest, FormDefinitionAdminDto>($"config/forms/{formId}", request, ct);

    public Task<HttpResponseWrapper<NoContent?>> DeleteFormAsync(int formId, CancellationToken ct = default)
        => _api.DeleteAsync($"config/forms/{formId}", ct);

    public Task<HttpResponseWrapper<FieldCatalogItemDto?>> CreateDynamicFieldAsync(CreateDynamicFieldRequest request, CancellationToken ct = default)
        => _api.PostAsync<CreateDynamicFieldRequest, FieldCatalogItemDto>("config/fields/dynamic", request, ct);

    public Task<HttpResponseWrapper<FieldCatalogItemDto?>> UpdateFieldAsync(int fieldId, UpdateFieldCatalogRequest request, CancellationToken ct = default)
        => _api.PutAsync<UpdateFieldCatalogRequest, FieldCatalogItemDto>($"config/fields/{fieldId}", request, ct);

    public Task<HttpResponseWrapper<NoContent?>> DeleteFieldAsync(int fieldId, CancellationToken ct = default)
        => _api.DeleteAsync($"config/fields/{fieldId}", ct);

    public Task<HttpResponseWrapper<FormFieldAdminDto?>> AddFieldToFormAsync(int formId, AddFieldToFormRequest request, CancellationToken ct = default)
        => _api.PostAsync<AddFieldToFormRequest, FormFieldAdminDto>($"config/forms/{formId}/fields", request, ct);

    public Task<HttpResponseWrapper<FormFieldAdminDto?>> UpdateFormFieldAsync(int formId, int formFieldId, UpdateFormFieldRequest request, CancellationToken ct = default)
        => _api.PutAsync<UpdateFormFieldRequest, FormFieldAdminDto>($"config/forms/{formId}/fields/{formFieldId}", request, ct);

    public Task<HttpResponseWrapper<List<CatalogAdminItemDto>?>> GetAdminCatalogAsync(string catalogKey, CancellationToken ct = default)
        => _api.GetAsync<List<CatalogAdminItemDto>>($"catalogs/admin/{Uri.EscapeDataString(catalogKey)}?cb={GetCacheBust()}", ct);

    public Task<HttpResponseWrapper<CatalogAdminItemDto?>> CreateAdminCatalogItemAsync(string catalogKey, UpsertCatalogItemRequest request, CancellationToken ct = default)
        => _api.PostAsync<UpsertCatalogItemRequest, CatalogAdminItemDto>($"catalogs/admin/{Uri.EscapeDataString(catalogKey)}", request, ct);

    public Task<HttpResponseWrapper<CatalogAdminItemDto?>> UpdateAdminCatalogItemAsync(string catalogKey, int id, UpsertCatalogItemRequest request, CancellationToken ct = default)
        => _api.PutAsync<UpsertCatalogItemRequest, CatalogAdminItemDto>($"catalogs/admin/{Uri.EscapeDataString(catalogKey)}/{id}", request, ct);

    public Task<HttpResponseWrapper<NoContent?>> DeleteAdminCatalogItemAsync(string catalogKey, int id, CancellationToken ct = default)
        => _api.DeleteAsync($"catalogs/admin/{Uri.EscapeDataString(catalogKey)}/{id}", ct);

    public Task<HttpResponseWrapper<List<DynamicFieldOptionDto>?>> GetFieldOptionsAsync(int fieldId, CancellationToken ct = default)
        => _api.GetAsync<List<DynamicFieldOptionDto>>($"config/fields/{fieldId}/options?cb={GetCacheBust()}", ct);

    public Task<HttpResponseWrapper<DynamicFieldOptionDto?>> CreateFieldOptionAsync(int fieldId, CreateDynamicFieldOptionRequest request, CancellationToken ct = default)
        => _api.PostAsync<CreateDynamicFieldOptionRequest, DynamicFieldOptionDto>($"config/fields/{fieldId}/options", request, ct);

    public Task<HttpResponseWrapper<DynamicFieldOptionDto?>> UpdateFieldOptionAsync(int fieldId, int optionId, UpdateDynamicFieldOptionRequest request, CancellationToken ct = default)
        => _api.PutAsync<UpdateDynamicFieldOptionRequest, DynamicFieldOptionDto>($"config/fields/{fieldId}/options/{optionId}", request, ct);

    public Task<HttpResponseWrapper<NoContent?>> DeleteFieldOptionAsync(int fieldId, int optionId, CancellationToken ct = default)
        => _api.DeleteAsync($"config/fields/{fieldId}/options/{optionId}", ct);

    public Task<HttpResponseWrapper<NoContent?>> DeleteFormFieldAsync(int formId, int formFieldId, CancellationToken ct = default)
        => _api.DeleteAsync($"config/forms/{formId}/fields/{formFieldId}", ct);

    private static long GetCacheBust() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
