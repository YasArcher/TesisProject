using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalogs;
using tesisproject.shared.DTOs.Configuration;

namespace tesisproject.frontend.Services.Implementations
{
    public class FormConfigurationClient : IFormConfigurationClient
    {
        private readonly IApiClient _api;

        public FormConfigurationClient(IApiClient api)
        {
            _api = api;
        }

        public async Task<List<FormSummaryDto>> GetFormsAsync(string? entityName = null, CancellationToken ct = default)
        {
            var url = $"api/config/forms?cb={GetCacheBust()}";
            if (!string.IsNullOrWhiteSpace(entityName))
            {
                url += $"&entityName={Uri.EscapeDataString(entityName)}";
            }

            return await _api.GetAsync<List<FormSummaryDto>>(url, ct) ?? new List<FormSummaryDto>();
        }

        public async Task<List<FieldCatalogItemDto>> GetFieldsAsync(string entityName, CancellationToken ct = default)
        {
            var url = $"api/config/fields?entityName={Uri.EscapeDataString(entityName)}&cb={GetCacheBust()}";
            return await _api.GetAsync<List<FieldCatalogItemDto>>(url, ct) ?? new List<FieldCatalogItemDto>();
        }

        public async Task<List<FormFieldAdminDto>> GetFormFieldsAsync(int formId, CancellationToken ct = default)
        {
            return await _api.GetAsync<List<FormFieldAdminDto>>($"api/config/forms/{formId}/fields?cb={GetCacheBust()}", ct)
                ?? new List<FormFieldAdminDto>();
        }

        public async Task<List<FieldCatalogItemDto>> GetDynamicFieldsAsync(string entityName, CancellationToken ct = default)
        {
            var url = $"api/config/fields/dynamic?entityName={Uri.EscapeDataString(entityName)}&cb={GetCacheBust()}";
            return await _api.GetAsync<List<FieldCatalogItemDto>>(url, ct) ?? new List<FieldCatalogItemDto>();
        }

        public Task<ResolvedFormDto?> GetResolvedFormAsync(string formKey, CancellationToken ct = default)
            => _api.GetAsync<ResolvedFormDto>($"api/config/forms/{Uri.EscapeDataString(formKey)}/resolved?cb={GetCacheBust()}", ct);

        public Task<ResolvedFormDto?> GetActiveResolvedFormAsync(string entityName, string? preferredFormKey = null, CancellationToken ct = default)
        {
            var url = $"api/config/forms/resolved-active?entityName={Uri.EscapeDataString(entityName)}&cb={GetCacheBust()}";
            if (!string.IsNullOrWhiteSpace(preferredFormKey))
            {
                url += $"&preferredFormKey={Uri.EscapeDataString(preferredFormKey)}";
            }

            return _api.GetAsync<ResolvedFormDto>(url, ct);
        }

        public async Task<List<CatalogItemDto>> GetCatalogItemsByFieldAsync(int fieldId, int? parentId = null, CancellationToken ct = default)
        {
            var url = $"api/config/fields/{fieldId}/catalog-items?cb={GetCacheBust()}";
            if (parentId.HasValue)
            {
                url += $"&parentId={parentId.Value}";
            }

            return await _api.GetAsync<List<CatalogItemDto>>(url, ct) ?? new List<CatalogItemDto>();
        }

        public Task<FormDefinitionAdminDto?> CreateFormAsync(CreateFormDefinitionRequest request, CancellationToken ct = default)
            => _api.PostAsync<CreateFormDefinitionRequest, FormDefinitionAdminDto>("api/config/forms", request, ct);

        public Task<FormDefinitionAdminDto?> UpdateFormAsync(int formId, UpdateFormDefinitionRequest request, CancellationToken ct = default)
            => _api.PutAsync<UpdateFormDefinitionRequest, FormDefinitionAdminDto>($"api/config/forms/{formId}", request, ct);

        public Task DeleteFormAsync(int formId, CancellationToken ct = default)
            => _api.DeleteAsync($"api/config/forms/{formId}", ct);

        public Task<FieldCatalogItemDto?> CreateDynamicFieldAsync(CreateDynamicFieldRequest request, CancellationToken ct = default)
            => _api.PostAsync<CreateDynamicFieldRequest, FieldCatalogItemDto>("api/config/fields/dynamic", request, ct);

        public Task<FieldCatalogItemDto?> UpdateFieldAsync(int fieldId, UpdateFieldCatalogRequest request, CancellationToken ct = default)
            => _api.PutAsync<UpdateFieldCatalogRequest, FieldCatalogItemDto>($"api/config/fields/{fieldId}", request, ct);

        public Task<FormFieldAdminDto?> AddFieldToFormAsync(int formId, AddFieldToFormRequest request, CancellationToken ct = default)
            => _api.PostAsync<AddFieldToFormRequest, FormFieldAdminDto>($"api/config/forms/{formId}/fields", request, ct);

        public Task<FormFieldAdminDto?> UpdateFormFieldAsync(int formId, int formFieldId, UpdateFormFieldRequest request, CancellationToken ct = default)
            => _api.PutAsync<UpdateFormFieldRequest, FormFieldAdminDto>($"api/config/forms/{formId}/fields/{formFieldId}", request, ct);

        public async Task<List<CatalogAdminItemDto>> GetAdminCatalogAsync(string catalogKey, CancellationToken ct = default)
        {
            return await _api.GetAsync<List<CatalogAdminItemDto>>($"api/catalogs/admin/{Uri.EscapeDataString(catalogKey)}?cb={GetCacheBust()}", ct)
                ?? new List<CatalogAdminItemDto>();
        }

        public Task<CatalogAdminItemDto?> CreateAdminCatalogItemAsync(string catalogKey, UpsertCatalogItemRequest request, CancellationToken ct = default)
            => _api.PostAsync<UpsertCatalogItemRequest, CatalogAdminItemDto>($"api/catalogs/admin/{Uri.EscapeDataString(catalogKey)}", request, ct);

        public Task<CatalogAdminItemDto?> UpdateAdminCatalogItemAsync(string catalogKey, int id, UpsertCatalogItemRequest request, CancellationToken ct = default)
            => _api.PutAsync<UpsertCatalogItemRequest, CatalogAdminItemDto>($"api/catalogs/admin/{Uri.EscapeDataString(catalogKey)}/{id}", request, ct);

        public async Task<List<DynamicFieldOptionDto>> GetFieldOptionsAsync(int fieldId, CancellationToken ct = default)
        {
            return await _api.GetAsync<List<DynamicFieldOptionDto>>($"api/config/fields/{fieldId}/options?cb={GetCacheBust()}", ct)
                ?? new List<DynamicFieldOptionDto>();
        }

        public Task<DynamicFieldOptionDto?> CreateFieldOptionAsync(int fieldId, CreateDynamicFieldOptionRequest request, CancellationToken ct = default)
            => _api.PostAsync<CreateDynamicFieldOptionRequest, DynamicFieldOptionDto>($"api/config/fields/{fieldId}/options", request, ct);

        public Task<DynamicFieldOptionDto?> UpdateFieldOptionAsync(int fieldId, int optionId, UpdateDynamicFieldOptionRequest request, CancellationToken ct = default)
            => _api.PutAsync<UpdateDynamicFieldOptionRequest, DynamicFieldOptionDto>($"api/config/fields/{fieldId}/options/{optionId}", request, ct);

        public Task DeleteFieldOptionAsync(int fieldId, int optionId, CancellationToken ct = default)
            => _api.DeleteAsync($"api/config/fields/{fieldId}/options/{optionId}", ct);

        public Task DeleteFormFieldAsync(int formId, int formFieldId, CancellationToken ct = default)
            => _api.DeleteAsync($"api/config/forms/{formId}/fields/{formFieldId}", ct);

        private static long GetCacheBust() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
