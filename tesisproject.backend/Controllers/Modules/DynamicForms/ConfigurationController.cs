using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Catalogs;
using tesisproject.shared.DTOs.Configuration;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/config")]
    [AllowAnonymous]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfigurationFormsService _configurationFormsService;

        public ConfigurationController(IConfigurationFormsService configurationFormsService)
        {
            _configurationFormsService = configurationFormsService;
        }

        [HttpGet("forms")]
        public async Task<ActionResult<List<FormSummaryDto>>> GetForms(
            [FromQuery] string? entityName,
            CancellationToken ct)
        {
            var forms = await _configurationFormsService.GetFormsAsync(entityName, ct);
            return Ok(forms);
        }

        [HttpGet("forms/{formId:int}/fields")]
        public async Task<ActionResult<List<FormFieldAdminDto>>> GetFormFields(int formId, CancellationToken ct)
        {
            var formFields = await _configurationFormsService.GetFormFieldsAsync(formId, ct);
            return Ok(formFields);
        }

        [HttpGet("fields")]
        public async Task<ActionResult<List<FieldCatalogItemDto>>> GetFields(
            [FromQuery] string entityName,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(entityName))
            {
                return BadRequest("entityName es obligatorio.");
            }

            var fields = await _configurationFormsService.GetFieldsByEntityAsync(entityName, ct);
            return Ok(fields);
        }

        [HttpGet("fields/dynamic")]
        public async Task<ActionResult<List<FieldCatalogItemDto>>> GetDynamicFields(
            [FromQuery] string entityName,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(entityName))
            {
                return BadRequest("entityName es obligatorio.");
            }

            var fields = await _configurationFormsService.GetDynamicFieldsByEntityAsync(entityName, ct);
            return Ok(fields);
        }

        [HttpGet("fields/{fieldId:int}/options")]
        public async Task<ActionResult<List<DynamicFieldOptionDto>>> GetFieldOptions(int fieldId, CancellationToken ct)
        {
            var options = await _configurationFormsService.GetFieldOptionsAsync(fieldId, ct);
            return Ok(options);
        }

        [HttpPost("fields/{fieldId:int}/options")]
        public async Task<ActionResult<DynamicFieldOptionDto>> CreateFieldOption(
            int fieldId,
            [FromBody] CreateDynamicFieldOptionRequest request,
            CancellationToken ct)
        {
            try
            {
                var option = await _configurationFormsService.CreateFieldOptionAsync(fieldId, request, ct);
                return Ok(option);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("fields/{fieldId:int}/options/{optionId:int}")]
        public async Task<ActionResult<DynamicFieldOptionDto>> UpdateFieldOption(
            int fieldId,
            int optionId,
            [FromBody] UpdateDynamicFieldOptionRequest request,
            CancellationToken ct)
        {
            var option = await _configurationFormsService.UpdateFieldOptionAsync(fieldId, optionId, request, ct);
            if (option is null)
            {
                return NotFound();
            }

            return Ok(option);
        }

        [HttpDelete("fields/{fieldId:int}/options/{optionId:int}")]
        public async Task<IActionResult> DeleteFieldOption(int fieldId, int optionId, CancellationToken ct)
        {
            var deleted = await _configurationFormsService.DeleteFieldOptionAsync(fieldId, optionId, ct);
            return deleted ? NoContent() : NotFound();
        }

        [HttpGet("fields/{fieldId:int}/catalog-items")]
        public async Task<ActionResult<List<CatalogItemDto>>> GetCatalogItems(
            int fieldId,
            [FromQuery] int? parentId,
            CancellationToken ct)
        {
            var items = await _configurationFormsService.GetCatalogItemsByFieldAsync(fieldId, parentId, ct);
            return Ok(items);
        }

        [HttpGet("forms/{formKey}/resolved")]
        public async Task<ActionResult<ResolvedFormDto>> GetResolvedForm(string formKey, CancellationToken ct)
        {
            var form = await _configurationFormsService.GetResolvedFormAsync(formKey, ct);
            if (form is null)
            {
                return NotFound();
            }

            return Ok(form);
        }

        [HttpGet("forms/resolved-active")]
        public async Task<ActionResult<ResolvedFormDto>> GetResolvedActiveForm(
            [FromQuery] string entityName,
            [FromQuery] string? preferredFormKey,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(entityName))
            {
                return BadRequest("entityName es obligatorio.");
            }

            var form = await _configurationFormsService.GetActiveResolvedFormAsync(entityName, preferredFormKey, ct);
            if (form is null)
            {
                return NotFound();
            }

            return Ok(form);
        }

        [HttpPost("forms")]
        public async Task<ActionResult<FormDefinitionAdminDto>> CreateForm(
            [FromBody] CreateFormDefinitionRequest request,
            CancellationToken ct)
        {
            try
            {
                var form = await _configurationFormsService.CreateFormAsync(request, ct);
                return Ok(form);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("forms/{formId:int}")]
        public async Task<ActionResult<FormDefinitionAdminDto>> UpdateForm(
            int formId,
            [FromBody] UpdateFormDefinitionRequest request,
            CancellationToken ct)
        {
            try
            {
                var form = await _configurationFormsService.UpdateFormAsync(formId, request, ct);
                if (form is null)
                {
                    return NotFound();
                }

                return Ok(form);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("forms/{formId:int}")]
        public async Task<IActionResult> DeleteForm(int formId, CancellationToken ct)
        {
            var deleted = await _configurationFormsService.DeleteFormAsync(formId, ct);
            return deleted ? NoContent() : NotFound();
        }

        [HttpPost("fields/dynamic")]
        public async Task<ActionResult<FieldCatalogItemDto>> CreateDynamicField(
            [FromBody] CreateDynamicFieldRequest request,
            CancellationToken ct)
        {
            try
            {
                var field = await _configurationFormsService.CreateDynamicFieldAsync(request, ct);
                return Ok(field);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("fields/{fieldId:int}")]
        public async Task<ActionResult<FieldCatalogItemDto>> UpdateField(
            int fieldId,
            [FromBody] UpdateFieldCatalogRequest request,
            CancellationToken ct)
        {
            var field = await _configurationFormsService.UpdateFieldAsync(fieldId, request, ct);
            if (field is null)
            {
                return NotFound();
            }

            return Ok(field);
        }

        [HttpPost("forms/{formId:int}/fields")]
        public async Task<ActionResult<FormFieldAdminDto>> AddFieldToForm(
            int formId,
            [FromBody] AddFieldToFormRequest request,
            CancellationToken ct)
        {
            try
            {
                var formField = await _configurationFormsService.AddFieldToFormAsync(formId, request, ct);
                return Ok(formField);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("forms/{formId:int}/fields/{formFieldId:int}")]
        public async Task<ActionResult<FormFieldAdminDto>> UpdateFormField(
            int formId,
            int formFieldId,
            [FromBody] UpdateFormFieldRequest request,
            CancellationToken ct)
        {
            var formField = await _configurationFormsService.UpdateFormFieldAsync(formId, formFieldId, request, ct);
            if (formField is null)
            {
                return NotFound();
            }

            return Ok(formField);
        }

        [HttpDelete("forms/{formId:int}/fields/{formFieldId:int}")]
        public async Task<IActionResult> DeleteFormField(int formId, int formFieldId, CancellationToken ct)
        {
            var deleted = await _configurationFormsService.RemoveFormFieldAsync(formId, formFieldId, ct);
            return deleted ? NoContent() : NotFound();
        }
    }
}
