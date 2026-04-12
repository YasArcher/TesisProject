using tesisproject.frontend.Services.Interfaces;
using tesisproject.frontend.Services.Platform.Api;
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
            => await RequireListAsync(
                GetFormsResultAsync(entityName, ct),
                "No pude cargar los formularios configurados.");

        public async Task<List<FieldCatalogItemDto>> GetFieldsAsync(string entityName, CancellationToken ct = default)
            => await RequireListAsync(
                GetFieldsResultAsync(entityName, ct),
                "No pude cargar los campos configurados.");

        public async Task<List<FormFieldAdminDto>> GetFormFieldsAsync(int formId, CancellationToken ct = default)
            => await RequireListAsync(
                GetFormFieldsResultAsync(formId, ct),
                "No pude cargar los campos del formulario.");

        public async Task<List<FieldCatalogItemDto>> GetDynamicFieldsAsync(string entityName, CancellationToken ct = default)
            => await RequireListAsync(
                GetDynamicFieldsResultAsync(entityName, ct),
                "No pude cargar los campos dinámicos.");

        public async Task<ResolvedFormDto?> GetResolvedFormAsync(string formKey, CancellationToken ct = default)
            => await RequireDataAsync(
                GetResolvedFormResultAsync(formKey, ct),
                "No pude cargar la definición resuelta del formulario.");

        public async Task<ResolvedFormDto?> GetActiveResolvedFormAsync(string entityName, string? preferredFormKey = null, CancellationToken ct = default)
        {
            return await RequireDataAsync(
                GetActiveResolvedFormResultAsync(entityName, preferredFormKey, ct),
                "No pude cargar la definición activa del formulario.");
        }

        public async Task<List<CatalogItemDto>> GetCatalogItemsByFieldAsync(int fieldId, int? parentId = null, CancellationToken ct = default)
            => await RequireListAsync(
                GetCatalogItemsByFieldResultAsync(fieldId, parentId, ct),
                "No pude cargar los ítems del catálogo.");

        public async Task<FormDefinitionAdminDto?> CreateFormAsync(CreateFormDefinitionRequest request, CancellationToken ct = default)
            => await RequireDataAsync(
                CreateFormResultAsync(request, ct),
                "No pude crear el formulario.");

        public async Task<FormDefinitionAdminDto?> UpdateFormAsync(int formId, UpdateFormDefinitionRequest request, CancellationToken ct = default)
            => await RequireDataAsync(
                UpdateFormResultAsync(formId, request, ct),
                "No pude actualizar el formulario.");

        public async Task DeleteFormAsync(int formId, CancellationToken ct = default)
            => await RequireSuccessAsync(
                DeleteFormResultAsync(formId, ct),
                "No pude eliminar el formulario.");

        public async Task<FieldCatalogItemDto?> CreateDynamicFieldAsync(CreateDynamicFieldRequest request, CancellationToken ct = default)
            => await RequireDataAsync(
                CreateDynamicFieldResultAsync(request, ct),
                "No pude crear el campo dinámico.");

        public async Task<FieldCatalogItemDto?> UpdateFieldAsync(int fieldId, UpdateFieldCatalogRequest request, CancellationToken ct = default)
            => await RequireDataAsync(
                UpdateFieldResultAsync(fieldId, request, ct),
                "No pude actualizar el campo.");

        public async Task<FormFieldAdminDto?> AddFieldToFormAsync(int formId, AddFieldToFormRequest request, CancellationToken ct = default)
            => await RequireDataAsync(
                AddFieldToFormResultAsync(formId, request, ct),
                "No pude agregar el campo al formulario.");

        public async Task<FormFieldAdminDto?> UpdateFormFieldAsync(int formId, int formFieldId, UpdateFormFieldRequest request, CancellationToken ct = default)
            => await RequireDataAsync(
                UpdateFormFieldResultAsync(formId, formFieldId, request, ct),
                "No pude actualizar la configuración del campo.");

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
            => await RequireListAsync(
                GetFieldOptionsResultAsync(fieldId, ct),
                "No pude cargar las opciones del campo.");

        public async Task<DynamicFieldOptionDto?> CreateFieldOptionAsync(int fieldId, CreateDynamicFieldOptionRequest request, CancellationToken ct = default)
            => await RequireDataAsync(
                CreateFieldOptionResultAsync(fieldId, request, ct),
                "No pude crear la opción del campo.");

        public async Task<DynamicFieldOptionDto?> UpdateFieldOptionAsync(int fieldId, int optionId, UpdateDynamicFieldOptionRequest request, CancellationToken ct = default)
            => await RequireDataAsync(
                UpdateFieldOptionResultAsync(fieldId, optionId, request, ct),
                "No pude actualizar la opción del campo.");

        public async Task DeleteFieldOptionAsync(int fieldId, int optionId, CancellationToken ct = default)
            => await RequireSuccessAsync(
                DeleteFieldOptionResultAsync(fieldId, optionId, ct),
                "No pude eliminar la opción del campo.");

        public async Task DeleteFormFieldAsync(int formId, int formFieldId, CancellationToken ct = default)
            => await RequireSuccessAsync(
                DeleteFormFieldResultAsync(formId, formFieldId, ct),
                "No pude quitar el campo del formulario.");

        public Task<HttpResponseWrapper<List<FormSummaryDto>?>> GetFormsResultAsync(string? entityName = null, CancellationToken ct = default)
        {
            var url = $"api/config/forms?cb={GetCacheBust()}";
            if (!string.IsNullOrWhiteSpace(entityName))
            {
                url += $"&entityName={Uri.EscapeDataString(entityName)}";
            }

            return _api.GetResultAsync<List<FormSummaryDto>>(url, ct);
        }

        public Task<HttpResponseWrapper<List<FormFieldAdminDto>?>> GetFormFieldsResultAsync(int formId, CancellationToken ct = default)
            => _api.GetResultAsync<List<FormFieldAdminDto>>($"api/config/forms/{formId}/fields?cb={GetCacheBust()}", ct);

        public Task<HttpResponseWrapper<List<FieldCatalogItemDto>?>> GetFieldsResultAsync(string entityName, CancellationToken ct = default)
            => _api.GetResultAsync<List<FieldCatalogItemDto>>($"api/config/fields?entityName={Uri.EscapeDataString(entityName)}&cb={GetCacheBust()}", ct);

        public Task<HttpResponseWrapper<List<FieldCatalogItemDto>?>> GetDynamicFieldsResultAsync(string entityName, CancellationToken ct = default)
            => _api.GetResultAsync<List<FieldCatalogItemDto>>($"api/config/fields/dynamic?entityName={Uri.EscapeDataString(entityName)}&cb={GetCacheBust()}", ct);

        public Task<HttpResponseWrapper<List<CatalogItemDto>?>> GetCatalogItemsByFieldResultAsync(int fieldId, int? parentId = null, CancellationToken ct = default)
        {
            var url = $"api/config/fields/{fieldId}/catalog-items?cb={GetCacheBust()}";
            if (parentId.HasValue)
            {
                url += $"&parentId={parentId.Value}";
            }

            return _api.GetResultAsync<List<CatalogItemDto>>(url, ct);
        }

        public Task<HttpResponseWrapper<List<DynamicFieldOptionDto>?>> GetFieldOptionsResultAsync(int fieldId, CancellationToken ct = default)
            => _api.GetResultAsync<List<DynamicFieldOptionDto>>($"api/config/fields/{fieldId}/options?cb={GetCacheBust()}", ct);

        public Task<HttpResponseWrapper<ResolvedFormDto?>> GetResolvedFormResultAsync(string formKey, CancellationToken ct = default)
            => _api.GetResultAsync<ResolvedFormDto>($"api/config/forms/{Uri.EscapeDataString(formKey)}/resolved?cb={GetCacheBust()}", ct);

        public Task<HttpResponseWrapper<ResolvedFormDto?>> GetActiveResolvedFormResultAsync(string entityName, string? preferredFormKey = null, CancellationToken ct = default)
        {
            var url = $"api/config/forms/resolved-active?entityName={Uri.EscapeDataString(entityName)}&cb={GetCacheBust()}";
            if (!string.IsNullOrWhiteSpace(preferredFormKey))
            {
                url += $"&preferredFormKey={Uri.EscapeDataString(preferredFormKey)}";
            }

            return _api.GetResultAsync<ResolvedFormDto>(url, ct);
        }

        public Task<HttpResponseWrapper<FormDefinitionAdminDto?>> CreateFormResultAsync(CreateFormDefinitionRequest request, CancellationToken ct = default)
            => _api.PostResultAsync<CreateFormDefinitionRequest, FormDefinitionAdminDto>("api/config/forms", request, ct);

        public Task<HttpResponseWrapper<FormDefinitionAdminDto?>> UpdateFormResultAsync(int formId, UpdateFormDefinitionRequest request, CancellationToken ct = default)
            => _api.PutResultAsync<UpdateFormDefinitionRequest, FormDefinitionAdminDto>($"api/config/forms/{formId}", request, ct);

        public Task<HttpResponseWrapper<object?>> DeleteFormResultAsync(int formId, CancellationToken ct = default)
            => _api.DeleteResultAsync<object>($"api/config/forms/{formId}", ct);

        public Task<HttpResponseWrapper<FieldCatalogItemDto?>> CreateDynamicFieldResultAsync(CreateDynamicFieldRequest request, CancellationToken ct = default)
            => _api.PostResultAsync<CreateDynamicFieldRequest, FieldCatalogItemDto>("api/config/fields/dynamic", request, ct);

        public Task<HttpResponseWrapper<FieldCatalogItemDto?>> UpdateFieldResultAsync(int fieldId, UpdateFieldCatalogRequest request, CancellationToken ct = default)
            => _api.PutResultAsync<UpdateFieldCatalogRequest, FieldCatalogItemDto>($"api/config/fields/{fieldId}", request, ct);

        public Task<HttpResponseWrapper<FormFieldAdminDto?>> AddFieldToFormResultAsync(int formId, AddFieldToFormRequest request, CancellationToken ct = default)
            => _api.PostResultAsync<AddFieldToFormRequest, FormFieldAdminDto>($"api/config/forms/{formId}/fields", request, ct);

        public Task<HttpResponseWrapper<FormFieldAdminDto?>> UpdateFormFieldResultAsync(int formId, int formFieldId, UpdateFormFieldRequest request, CancellationToken ct = default)
            => _api.PutResultAsync<UpdateFormFieldRequest, FormFieldAdminDto>($"api/config/forms/{formId}/fields/{formFieldId}", request, ct);

        public Task<HttpResponseWrapper<object?>> DeleteFormFieldResultAsync(int formId, int formFieldId, CancellationToken ct = default)
            => _api.DeleteResultAsync<object>($"api/config/forms/{formId}/fields/{formFieldId}", ct);

        public Task<HttpResponseWrapper<DynamicFieldOptionDto?>> CreateFieldOptionResultAsync(int fieldId, CreateDynamicFieldOptionRequest request, CancellationToken ct = default)
            => _api.PostResultAsync<CreateDynamicFieldOptionRequest, DynamicFieldOptionDto>($"api/config/fields/{fieldId}/options", request, ct);

        public Task<HttpResponseWrapper<DynamicFieldOptionDto?>> UpdateFieldOptionResultAsync(int fieldId, int optionId, UpdateDynamicFieldOptionRequest request, CancellationToken ct = default)
            => _api.PutResultAsync<UpdateDynamicFieldOptionRequest, DynamicFieldOptionDto>($"api/config/fields/{fieldId}/options/{optionId}", request, ct);

        public Task<HttpResponseWrapper<object?>> DeleteFieldOptionResultAsync(int fieldId, int optionId, CancellationToken ct = default)
            => _api.DeleteResultAsync<object>($"api/config/fields/{fieldId}/options/{optionId}", ct);

        private static long GetCacheBust() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        private static async Task<T?> RequireDataAsync<T>(Task<HttpResponseWrapper<T?>> resultTask, string fallbackMessage)
        {
            var result = await resultTask;
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Message ?? fallbackMessage);
            }

            return result.Data;
        }

        private static async Task<List<T>> RequireListAsync<T>(Task<HttpResponseWrapper<List<T>?>> resultTask, string fallbackMessage)
        {
            var result = await resultTask;
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Message ?? fallbackMessage);
            }

            return result.Data ?? new List<T>();
        }

        private static async Task RequireSuccessAsync(Task<HttpResponseWrapper<object?>> resultTask, string fallbackMessage)
        {
            var result = await resultTask;
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Message ?? fallbackMessage);
            }
        }
    }
}
