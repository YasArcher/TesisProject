using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Catalogs;
using tesisproject.shared.DTOs.Configuration;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified;

[ApiController]
[Route("api/config")]
[Route("api/scientific-production/config")]
[Authorize]
public sealed class UnifiedArticlesConfigurationController(IUnifiedArticleConfigurationService service) : ControllerBase
{
    [HttpGet("forms")]
    public async Task<ActionResult<ServiceResult<List<FormSummaryDto>>>> GetForms([FromQuery] string? entityName,
        CancellationToken ct)
        => (await service.GetForms(entityName, ct)).ToActionResult();

    [HttpGet("forms/{formId:int}/fields")]
    public async Task<ActionResult<ServiceResult<List<FormFieldAdminDto>>>> GetFormFields(int formId, CancellationToken ct)
        => (await service.GetFormFields(formId, ct)).ToActionResult();

    [HttpGet("fields")]
    public async Task<ActionResult<ServiceResult<List<FieldCatalogItemDto>>>> GetFields([FromQuery] string entityName,
        CancellationToken ct)
        => (await service.GetFields(entityName, ct)).ToActionResult();

    [HttpGet("fields/dynamic")]
    public async Task<ActionResult<ServiceResult<List<FieldCatalogItemDto>>>> GetDynamicFields([FromQuery] string entityName,
        CancellationToken ct)
        => (await service.GetDynamicFields(entityName, ct)).ToActionResult();

    [HttpGet("fields/{fieldId:int}/options")]
    public async Task<ActionResult<ServiceResult<List<DynamicFieldOptionDto>>>> GetFieldOptions(int fieldId, CancellationToken ct)
        => (await service.GetFieldOptions(fieldId, ct)).ToActionResult();

    [HttpGet("fields/{fieldId:int}/catalog-items")]
    public async Task<ActionResult<ServiceResult<List<CatalogItemDto>>>> GetCatalogItems(int fieldId, [FromQuery] int? parentId, CancellationToken ct)
        => (await service.GetCatalogItems(fieldId, parentId, ct)).ToActionResult();

    [HttpGet("forms/{formKey}/resolved")]
    public async Task<ActionResult<ServiceResult<ResolvedFormDto>>> GetResolvedForm(string formKey, CancellationToken ct)
        => (await service.GetResolvedForm(formKey, ct)).ToActionResult();

    [HttpGet("forms/resolved-active")]
    public async Task<ActionResult<ServiceResult<ResolvedFormDto>>> GetResolvedActiveForm([FromQuery] string entityName,
        [FromQuery] string? preferredFormKey,
        CancellationToken ct)
        => (await service.GetResolvedActiveForm(entityName, preferredFormKey, ct)).ToActionResult();

    [HttpPost("forms")]
    public async Task<ActionResult<ServiceResult<FormDefinitionAdminDto>>> CreateForm([FromBody] CreateFormDefinitionRequest request,
        CancellationToken ct)
        => (await service.CreateForm(request, ct)).ToActionResult();

    [HttpPut("forms/{formId:int}")]
    public async Task<ActionResult<ServiceResult<FormDefinitionAdminDto>>> UpdateForm(int formId,
        [FromBody] UpdateFormDefinitionRequest request,
        CancellationToken ct)
        => (await service.UpdateForm(formId, request, ct)).ToActionResult();

    [HttpDelete("forms/{formId:int}")]
    public async Task<ActionResult<ServiceResult<NoContent>>> DeleteForm(int formId, CancellationToken ct)
        => (await service.DeleteForm(formId, ct)).ToActionResult();

    [HttpPost("fields/dynamic")]
    public async Task<ActionResult<ServiceResult<FieldCatalogItemDto>>> CreateDynamicField([FromBody] CreateDynamicFieldRequest request,
        CancellationToken ct)
        => (await service.CreateDynamicField(request, ct)).ToActionResult();

    [HttpPut("fields/{fieldId:int}")]
    public async Task<ActionResult<ServiceResult<FieldCatalogItemDto>>> UpdateField(int fieldId,
        [FromBody] UpdateFieldCatalogRequest request,
        CancellationToken ct)
        => (await service.UpdateField(fieldId, request, ct)).ToActionResult();

    [HttpDelete("fields/{fieldId:int}")]
    public async Task<ActionResult<ServiceResult<NoContent>>> DeleteField(int fieldId, CancellationToken ct)
        => (await service.DeleteField(fieldId, ct)).ToActionResult();

    [HttpPost("forms/{formId:int}/fields")]
    public async Task<ActionResult<ServiceResult<FormFieldAdminDto>>> AddFieldToForm(int formId,
        [FromBody] AddFieldToFormRequest request,
        CancellationToken ct)
        => (await service.AddFieldToForm(formId, request, ct)).ToActionResult();

    [HttpPut("forms/{formId:int}/fields/{formFieldId:int}")]
    public async Task<ActionResult<ServiceResult<FormFieldAdminDto>>> UpdateFormField(int formId,
        int formFieldId,
        [FromBody] UpdateFormFieldRequest request,
        CancellationToken ct)
        => (await service.UpdateFormField(formId, formFieldId, request, ct)).ToActionResult();

    [HttpDelete("forms/{formId:int}/fields/{formFieldId:int}")]
    public async Task<ActionResult<ServiceResult<NoContent>>> DeleteFormField(int formId, int formFieldId, CancellationToken ct)
        => (await service.DeleteFormField(formId, formFieldId, ct)).ToActionResult();

    [HttpPost("fields/{fieldId:int}/options")]
    public async Task<ActionResult<ServiceResult<DynamicFieldOptionDto>>> CreateFieldOption(int fieldId,
        [FromBody] CreateDynamicFieldOptionRequest request,
        CancellationToken ct)
        => (await service.CreateFieldOption(fieldId, request, ct)).ToActionResult();

    [HttpPut("fields/{fieldId:int}/options/{optionId:int}")]
    public async Task<ActionResult<ServiceResult<DynamicFieldOptionDto>>> UpdateFieldOption(int fieldId,
        int optionId,
        [FromBody] UpdateDynamicFieldOptionRequest request,
        CancellationToken ct)
        => (await service.UpdateFieldOption(fieldId, optionId, request, ct)).ToActionResult();

    [HttpDelete("fields/{fieldId:int}/options/{optionId:int}")]
    public async Task<ActionResult<ServiceResult<NoContent>>> DeleteFieldOption(int fieldId, int optionId, CancellationToken ct)
        => (await service.DeleteFieldOption(fieldId, optionId, ct)).ToActionResult();

}

[ApiController]
[Route("api/catalogs")]
[Route("api/scientific-production/catalogs")]
[Authorize]
public sealed class UnifiedArticlesCatalogsController(IUnifiedArticleConfigurationService service) : ControllerBase
{
    [HttpGet("admin/{catalogKey}")]
    public async Task<ActionResult<ServiceResult<List<CatalogAdminItemDto>>>> GetAdminCatalog(string catalogKey, CancellationToken ct)
        => (await service.GetAdminCatalog(catalogKey, ct)).ToActionResult();

}

