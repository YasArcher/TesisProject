using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Convocation.Request;
using tesisproject.shared.DTOs.Convocation.Response;
using tesisproject.shared.DTOs.ConvocationRule.Request;
using tesisproject.shared.DTOs.ConvocationRule.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ConvocationsController : ControllerBase
    {
        private readonly IConvocationService _service;

        public ConvocationsController(IConvocationService service) => _service = service;

        // ===================== CONVOCATIONS =====================

        // GET: api/convocations
        [HttpGet]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<ConvocationListItemResponseDTO>>>> GetAll(
            CancellationToken ct = default)
            => (await _service.ListAsync(ct)).ToActionResult();

        // GET: api/convocations/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<ConvocationDetailResponseDTO>>> GetById(
            int id,
            CancellationToken ct = default)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // POST: api/convocations
        [HttpPost]
        public async Task<ActionResult<ServiceResult<ConvocationDetailResponseDTO>>> Create(
            [FromBody] ConvocationCreateRequestDTO body,
            CancellationToken ct = default)
            => (await _service.CreateAsync(body, ct)).ToActionResult();

        // PUT: api/convocations/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServiceResult<ConvocationDetailResponseDTO>>> Update(
            int id,
            [FromBody] ConvocationUpdateRequestDTO body,
            CancellationToken ct = default)
        {
            body.Id = id; // el id de la ruta manda
            return (await _service.UpdateAsync(body, ct)).ToActionResult();
        }

        // POST: api/convocations/{id}/activate
        [HttpPost("{id:int}/activate")]
        public async Task<ActionResult<ServiceResult<NoContent>>> ActivateExclusive(
            int id,
            CancellationToken ct = default)
            => (await _service.ActivateExclusiveAsync(id, ct)).ToActionResult();

        // ======================== RULES ========================

        // POST: api/convocations/{id}/rules
        [HttpPost("{id:int}/rules")]
        public async Task<ActionResult<ServiceResult<ConvocationRuleResponseDTO>>> AddRule(
            int id,
            [FromBody] ConvocationRuleCreateRequestDTO body,
            CancellationToken ct = default)
        {
            body.ConvocationId = id; // ruta manda
            return (await _service.AddRuleAsync(body, ct)).ToActionResult();
        }

        // PUT: api/convocations/{id}/rules/{ruleId}
        [HttpPut("{id:int}/rules/{ruleId:int}")]
        public async Task<ActionResult<ServiceResult<ConvocationRuleResponseDTO>>> UpdateRule(
            int id,
            int ruleId,
            [FromBody] ConvocationRuleUpdateRequestDTO body,
            CancellationToken ct = default)
        {
            body.ConvocationId = id; // ruta manda
            body.Id = ruleId;
            return (await _service.UpdateRuleAsync(body, ct)).ToActionResult();
        }

        // DELETE: api/convocations/{id}/rules/{ruleId}
        [HttpDelete("{id:int}/rules/{ruleId:int}")]
        public async Task<ActionResult<ServiceResult<NoContent>>> RemoveRule(
            int id,
            int ruleId,
            CancellationToken ct = default)
            => (await _service.RemoveRuleAsync(id, ruleId, ct)).ToActionResult();
    }
}