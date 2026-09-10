using tesisproject.shared.DTOs.Catalogs;
using tesisproject.shared.DTOs.Configuration;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces;

public interface IArticleConfigurationService
{
    Task<ServiceResult<List<FormSummaryDto>>> GetForms(string? entityName,
        CancellationToken ct);
    Task<ServiceResult<List<FormFieldAdminDto>>> GetFormFields(int formId, CancellationToken ct);
    Task<ServiceResult<List<FieldCatalogItemDto>>> GetFields(string entityName,
        CancellationToken ct);
    Task<ServiceResult<List<FieldCatalogItemDto>>> GetDynamicFields(string entityName,
        CancellationToken ct);
    Task<ServiceResult<List<DynamicFieldOptionDto>>> GetFieldOptions(int fieldId, CancellationToken ct);
    Task<ServiceResult<List<CatalogItemDto>>> GetCatalogItems(int fieldId, int? parentId, CancellationToken ct);
    Task<ServiceResult<ResolvedFormDto>> GetResolvedForm(string formKey, CancellationToken ct);
    Task<ServiceResult<ResolvedFormDto>> GetResolvedActiveForm(string entityName,
        string? preferredFormKey,
        CancellationToken ct);
    Task<ServiceResult<FormDefinitionAdminDto>> CreateForm(CreateFormDefinitionRequest request,
        CancellationToken ct);
    Task<ServiceResult<FormDefinitionAdminDto>> UpdateForm(int formId,
        UpdateFormDefinitionRequest request,
        CancellationToken ct);
    Task<ServiceResult<NoContent>> DeleteForm(int formId, CancellationToken ct);
    Task<ServiceResult<FieldCatalogItemDto>> CreateDynamicField(CreateDynamicFieldRequest request,
        CancellationToken ct);
    Task<ServiceResult<FieldCatalogItemDto>> UpdateField(int fieldId,
        UpdateFieldCatalogRequest request,
        CancellationToken ct);
    Task<ServiceResult<NoContent>> DeleteField(int fieldId, CancellationToken ct);
    Task<ServiceResult<FormFieldAdminDto>> AddFieldToForm(int formId,
        AddFieldToFormRequest request,
        CancellationToken ct);
    Task<ServiceResult<FormFieldAdminDto>> UpdateFormField(int formId,
        int formFieldId,
        UpdateFormFieldRequest request,
        CancellationToken ct);
    Task<ServiceResult<NoContent>> DeleteFormField(int formId, int formFieldId, CancellationToken ct);
    Task<ServiceResult<DynamicFieldOptionDto>> CreateFieldOption(int fieldId,
        CreateDynamicFieldOptionRequest request,
        CancellationToken ct);
    Task<ServiceResult<DynamicFieldOptionDto>> UpdateFieldOption(int fieldId,
        int optionId,
        UpdateDynamicFieldOptionRequest request,
        CancellationToken ct);
    Task<ServiceResult<NoContent>> DeleteFieldOption(int fieldId, int optionId, CancellationToken ct);
    Task<ServiceResult<List<CatalogAdminItemDto>>> GetAdminCatalog(string catalogKey, CancellationToken ct);
}
