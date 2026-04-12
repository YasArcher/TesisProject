using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Wrappers;
using tesisproject.shared.DTOs.Catalogs;
using tesisproject.shared.DTOs.Configuration;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/config")]
    [Authorize(Policy = AppPolicies.AuthenticatedUser)]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfigurationFormsService _configurationFormsService;

        public ConfigurationController(IConfigurationFormsService configurationFormsService)
        {
            _configurationFormsService = configurationFormsService;
        }

        [HttpGet("forms")]
        public async Task<ActionResult<ApiResult<List<FormSummaryDto>>>> GetForms(
            [FromQuery] string? entityName,
            CancellationToken ct)
        {
            var forms = await _configurationFormsService.GetFormsAsync(entityName, ct);
            return Ok(ApiResult<List<FormSummaryDto>>.Success(forms));
        }

        [HttpGet("forms/{formId:int}/fields")]
        public async Task<ActionResult<ApiResult<List<FormFieldAdminDto>>>> GetFormFields(int formId, CancellationToken ct)
        {
            var formFields = await _configurationFormsService.GetFormFieldsAsync(formId, ct);
            return Ok(ApiResult<List<FormFieldAdminDto>>.Success(formFields));
        }

        [HttpGet("fields")]
        public async Task<ActionResult<ApiResult<List<FieldCatalogItemDto>>>> GetFields(
            [FromQuery] string entityName,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(entityName))
            {
                return BadRequest(ApiResult<List<FieldCatalogItemDto>>.Fail("entityName es obligatorio."));
            }

            var fields = await _configurationFormsService.GetFieldsByEntityAsync(entityName, ct);
            return Ok(ApiResult<List<FieldCatalogItemDto>>.Success(fields));
        }

        [HttpGet("fields/dynamic")]
        public async Task<ActionResult<ApiResult<List<FieldCatalogItemDto>>>> GetDynamicFields(
            [FromQuery] string entityName,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(entityName))
            {
                return BadRequest(ApiResult<List<FieldCatalogItemDto>>.Fail("entityName es obligatorio."));
            }

            var fields = await _configurationFormsService.GetDynamicFieldsByEntityAsync(entityName, ct);
            return Ok(ApiResult<List<FieldCatalogItemDto>>.Success(fields));
        }

        [HttpGet("fields/{fieldId:int}/options")]
        public async Task<ActionResult<ApiResult<List<DynamicFieldOptionDto>>>> GetFieldOptions(int fieldId, CancellationToken ct)
        {
            var options = await _configurationFormsService.GetFieldOptionsAsync(fieldId, ct);
            return Ok(ApiResult<List<DynamicFieldOptionDto>>.Success(options));
        }

        [HttpPost("fields/{fieldId:int}/options")]
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
        public async Task<ActionResult<ApiResult<DynamicFieldOptionDto>>> CreateFieldOption(
            int fieldId,
            [FromBody] CreateDynamicFieldOptionRequest request,
            CancellationToken ct)
        {
            try
            {
                var option = await _configurationFormsService.CreateFieldOptionAsync(fieldId, request, ct);
                return Ok(ApiResult<DynamicFieldOptionDto>.Success(option));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResult<DynamicFieldOptionDto>.Fail(ex.Message));
            }
        }

        [HttpPut("fields/{fieldId:int}/options/{optionId:int}")]
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
        public async Task<ActionResult<ApiResult<DynamicFieldOptionDto>>> UpdateFieldOption(
            int fieldId,
            int optionId,
            [FromBody] UpdateDynamicFieldOptionRequest request,
            CancellationToken ct)
        {
            var option = await _configurationFormsService.UpdateFieldOptionAsync(fieldId, optionId, request, ct);
            if (option is null)
            {
                return NotFound(ApiResult<DynamicFieldOptionDto>.Fail("No encontré la opción que intentas actualizar."));
            }

            return Ok(ApiResult<DynamicFieldOptionDto>.Success(option));
        }

        [HttpDelete("fields/{fieldId:int}/options/{optionId:int}")]
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
        public async Task<ActionResult<ApiResult>> DeleteFieldOption(int fieldId, int optionId, CancellationToken ct)
        {
            var deleted = await _configurationFormsService.DeleteFieldOptionAsync(fieldId, optionId, ct);
            return deleted
                ? Ok(ApiResult.Success())
                : NotFound(ApiResult.Fail("No encontré la opción que intentas eliminar."));
        }

        [HttpGet("fields/{fieldId:int}/catalog-items")]
        public async Task<ActionResult<ApiResult<List<CatalogItemDto>>>> GetCatalogItems(
            int fieldId,
            [FromQuery] int? parentId,
            CancellationToken ct)
        {
            var items = await _configurationFormsService.GetCatalogItemsByFieldAsync(fieldId, parentId, ct);
            return Ok(ApiResult<List<CatalogItemDto>>.Success(items));
        }

        [HttpGet("forms/{formKey}/resolved")]
        public async Task<ActionResult<ApiResult<ResolvedFormDto>>> GetResolvedForm(string formKey, CancellationToken ct)
        {
            var form = await _configurationFormsService.GetResolvedFormAsync(formKey, ct);
            if (form is null)
            {
                return NotFound(ApiResult<ResolvedFormDto>.Fail("No encontré el formulario solicitado."));
            }

            return Ok(ApiResult<ResolvedFormDto>.Success(form));
        }

        [HttpGet("forms/resolved-active")]
        public async Task<ActionResult<ApiResult<ResolvedFormDto>>> GetResolvedActiveForm(
            [FromQuery] string entityName,
            [FromQuery] string? preferredFormKey,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(entityName))
            {
                return BadRequest(ApiResult<ResolvedFormDto>.Fail("entityName es obligatorio."));
            }

            var form = await _configurationFormsService.GetActiveResolvedFormAsync(entityName, preferredFormKey, ct);
            if (form is null)
            {
                return NotFound(ApiResult<ResolvedFormDto>.Fail("No encontré un formulario activo para la entidad solicitada."));
            }

            return Ok(ApiResult<ResolvedFormDto>.Success(form));
        }

        [HttpPost("forms")]
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
        public async Task<ActionResult<ApiResult<FormDefinitionAdminDto>>> CreateForm(
            [FromBody] CreateFormDefinitionRequest request,
            CancellationToken ct)
        {
            try
            {
                var form = await _configurationFormsService.CreateFormAsync(request, ct);
                return Ok(ApiResult<FormDefinitionAdminDto>.Success(form));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResult<FormDefinitionAdminDto>.Fail(ex.Message));
            }
        }

        [HttpPut("forms/{formId:int}")]
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
        public async Task<ActionResult<ApiResult<FormDefinitionAdminDto>>> UpdateForm(
            int formId,
            [FromBody] UpdateFormDefinitionRequest request,
            CancellationToken ct)
        {
            try
            {
                var form = await _configurationFormsService.UpdateFormAsync(formId, request, ct);
                if (form is null)
                {
                    return NotFound(ApiResult<FormDefinitionAdminDto>.Fail("No encontré el formulario que intentas actualizar."));
                }

                return Ok(ApiResult<FormDefinitionAdminDto>.Success(form));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResult<FormDefinitionAdminDto>.Fail(ex.Message));
            }
        }

        [HttpDelete("forms/{formId:int}")]
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
        public async Task<ActionResult<ApiResult>> DeleteForm(int formId, CancellationToken ct)
        {
            var deleted = await _configurationFormsService.DeleteFormAsync(formId, ct);
            return deleted
                ? Ok(ApiResult.Success())
                : NotFound(ApiResult.Fail("No encontré el formulario que intentas eliminar."));
        }

        [HttpPost("fields/dynamic")]
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
        public async Task<ActionResult<ApiResult<FieldCatalogItemDto>>> CreateDynamicField(
            [FromBody] CreateDynamicFieldRequest request,
            CancellationToken ct)
        {
            try
            {
                var field = await _configurationFormsService.CreateDynamicFieldAsync(request, ct);
                return Ok(ApiResult<FieldCatalogItemDto>.Success(field));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResult<FieldCatalogItemDto>.Fail(ex.Message));
            }
        }

        [HttpPut("fields/{fieldId:int}")]
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
        public async Task<ActionResult<ApiResult<FieldCatalogItemDto>>> UpdateField(
            int fieldId,
            [FromBody] UpdateFieldCatalogRequest request,
            CancellationToken ct)
        {
            var field = await _configurationFormsService.UpdateFieldAsync(fieldId, request, ct);
            if (field is null)
            {
                return NotFound(ApiResult<FieldCatalogItemDto>.Fail("No encontré el campo que intentas actualizar."));
            }

            return Ok(ApiResult<FieldCatalogItemDto>.Success(field));
        }

        [HttpPost("forms/{formId:int}/fields")]
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
        public async Task<ActionResult<ApiResult<FormFieldAdminDto>>> AddFieldToForm(
            int formId,
            [FromBody] AddFieldToFormRequest request,
            CancellationToken ct)
        {
            try
            {
                var formField = await _configurationFormsService.AddFieldToFormAsync(formId, request, ct);
                return Ok(ApiResult<FormFieldAdminDto>.Success(formField));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResult<FormFieldAdminDto>.Fail(ex.Message));
            }
        }

        [HttpPut("forms/{formId:int}/fields/{formFieldId:int}")]
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
        public async Task<ActionResult<ApiResult<FormFieldAdminDto>>> UpdateFormField(
            int formId,
            int formFieldId,
            [FromBody] UpdateFormFieldRequest request,
            CancellationToken ct)
        {
            var formField = await _configurationFormsService.UpdateFormFieldAsync(formId, formFieldId, request, ct);
            if (formField is null)
            {
                return NotFound(ApiResult<FormFieldAdminDto>.Fail("No encontré el campo del formulario que intentas actualizar."));
            }

            return Ok(ApiResult<FormFieldAdminDto>.Success(formField));
        }

        [HttpDelete("forms/{formId:int}/fields/{formFieldId:int}")]
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
        public async Task<ActionResult<ApiResult>> DeleteFormField(int formId, int formFieldId, CancellationToken ct)
        {
            var deleted = await _configurationFormsService.RemoveFormFieldAsync(formId, formFieldId, ct);
            return deleted
                ? Ok(ApiResult.Success())
                : NotFound(ApiResult.Fail("No encontré el campo del formulario que intentas quitar."));
        }
    }
}
