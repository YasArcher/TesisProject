using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Workflow;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/workflows/import-batches")]
    [Authorize(Policy = AppPolicies.WorkflowAccess)]
    public class WorkflowController : ControllerBase
    {
        private readonly IWorkflowService _service;

        public WorkflowController(IWorkflowService service)
        {
            _service = service;
        }

        [HttpGet("{batchId:int}")]
        public async Task<ActionResult<WorkflowBatchDetailDto>> GetByBatch(int batchId, CancellationToken ct = default)
        {
            try
            {
                var workflow = await _service.GetBatchWorkflowAsync(batchId, ct);
                return workflow is null ? NotFound() : Ok(workflow);
            }
            catch (Exception ex)
            {
                return Problem(title: "No pude cargar el workflow del lote.", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("inbox/review")]
        [Authorize(Policy = AppPolicies.WorkflowReview)]
        public async Task<ActionResult<List<WorkflowInboxItemDto>>> GetReviewInbox([FromQuery] int take = 50, CancellationToken ct = default)
        {
            try
            {
                var roleNames = User?.Claims
                    .Where(x => x.Type == ClaimTypes.Role)
                    .Select(x => x.Value)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList()
                    ?? new List<string>();

                return Ok(await _service.GetReviewInboxAsync(GetCurrentUserId(), roleNames, take, ct));
            }
            catch (Exception ex)
            {
                return Problem(title: "No pude cargar la bandeja de revisión.", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("inbox/author")]
        public async Task<ActionResult<List<WorkflowInboxItemDto>>> GetAuthorInbox([FromQuery] int take = 50, CancellationToken ct = default)
        {
            try
            {
                return Ok(await _service.GetAuthorInboxAsync(GetCurrentUserId(), take, ct));
            }
            catch (Exception ex)
            {
                return Problem(title: "No pude cargar la bandeja del autor.", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("{batchId:int}/claim")]
        [Authorize(Policy = AppPolicies.WorkflowReview)]
        public async Task<ActionResult<WorkflowBatchDetailDto>> Claim(int batchId, [FromBody] WorkflowActionRequest? request, CancellationToken ct = default)
        {
            try
            {
                return Ok(await _service.ClaimCurrentStageAsync(batchId, GetCurrentUserId(), GetCurrentRoleNames(), request?.Comments, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{batchId:int}/return")]
        [Authorize(Policy = AppPolicies.WorkflowReview)]
        public async Task<ActionResult<WorkflowBatchDetailDto>> Return(int batchId, [FromBody] WorkflowActionRequest request, CancellationToken ct = default)
        {
            try
            {
                return Ok(await _service.ReturnCurrentStageAsync(batchId, GetCurrentUserId(), GetCurrentRoleNames(), request?.Comments, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{batchId:int}/approve")]
        [Authorize(Policy = AppPolicies.WorkflowReview)]
        public async Task<ActionResult<WorkflowBatchDetailDto>> Approve(int batchId, [FromBody] WorkflowActionRequest request, CancellationToken ct = default)
        {
            try
            {
                return Ok(await _service.ApproveCurrentStageAsync(batchId, GetCurrentUserId(), GetCurrentRoleNames(), request?.Comments, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private string? GetCurrentUserId()
        {
            return User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User?.Identity?.Name;
        }

        private IReadOnlyCollection<string> GetCurrentRoleNames()
        {
            return User?.Claims
                .Where(x => x.Type == ClaimTypes.Role)
                .Select(x => x.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList()
                ?? new List<string>();
        }
    }
}
