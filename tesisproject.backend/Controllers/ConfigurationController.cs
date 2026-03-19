using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
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
    }
}
