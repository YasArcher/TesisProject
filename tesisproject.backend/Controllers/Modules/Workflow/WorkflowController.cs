using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Imports;
using tesisproject.shared.DTOs.Workflow;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/workflows/import-batches")]
    [Route("api/scientific-production/workflows/import-batches")]
    [Authorize(Policy = AppPolicies.WorkflowAccess)]
    public class WorkflowController : ControllerBase
    {
        private readonly IWorkflowService _service;
        private readonly IBulkImportService _bulkImportService;

        public WorkflowController(IWorkflowService service, IBulkImportService bulkImportService)
        {
            _service = service;
            _bulkImportService = bulkImportService;
        }

        [HttpGet("{batchId:int}")]
        public async Task<ActionResult<WorkflowBatchDetailDto>> GetByBatch(int batchId, CancellationToken ct = default)
        {
            try
            {
                if (!await CanAccessBatchWorkflowAsync(batchId, ct))
                {
                    return Forbid();
                }

                var workflow = await _service.GetBatchWorkflowAsync(batchId, ct);
                return workflow is null ? NotFound() : Ok(workflow);
            }
            catch (Exception ex)
            {
                return Problem(title: "No pude cargar el workflow del lote.", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("{batchId:int}/preview")]
        public async Task<ActionResult<BulkImportBatchDetailDto>> GetBatchPreview(int batchId, [FromQuery] int previewRows = 50, CancellationToken ct = default)
        {
            try
            {
                if (!await CanAccessBatchWorkflowAsync(batchId, ct))
                {
                    return Forbid();
                }

                var detail = await _bulkImportService.GetBatchAsync(batchId, previewRows, ct);
                return detail is null ? NotFound() : Ok(detail);
            }
            catch (Exception ex)
            {
                return Problem(title: "No pude abrir la previsualización del lote.", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("{batchId:int}/validate")]
        public async Task<ActionResult<BulkImportActionResultDto>> ValidateBatch(int batchId, CancellationToken ct = default)
        {
            try
            {
                if (!await CanAccessBatchWorkflowAsync(batchId, ct, authorRequiresReturnedStatus: true))
                {
                    return Forbid();
                }

                return Ok(await _bulkImportService.ValidateBatchAsync(batchId, ct));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{batchId:int}/rows/{rowId:int}")]
        public async Task<ActionResult<BulkImportActionResultDto>> CorrectRow(int batchId, int rowId, [FromBody] BulkImportRowCorrectionRequest request, CancellationToken ct = default)
        {
            try
            {
                if (!await CanAccessBatchWorkflowAsync(batchId, ct, authorRequiresReturnedStatus: true))
                {
                    return Forbid();
                }

                return Ok(await _bulkImportService.CorrectRowAsync(batchId, rowId, request, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Problem(title: "No pude corregir la fila del envío.", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
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

        [HttpPost("{batchId:int}/return-to-uodide")]
        [Authorize(Policy = AppPolicies.WorkflowReview)]
        public async Task<ActionResult<WorkflowBatchDetailDto>> ReturnToUodide(int batchId, [FromBody] WorkflowActionRequest request, CancellationToken ct = default)
        {
            try
            {
                return Ok(await _service.ReturnCurrentStageToPreviousStageAsync(batchId, GetCurrentUserId(), GetCurrentRoleNames(), request?.Comments, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{batchId:int}/decline")]
        [Authorize(Policy = AppPolicies.WorkflowReview)]
        public async Task<ActionResult<WorkflowBatchDetailDto>> Decline(int batchId, [FromBody] WorkflowActionRequest request, CancellationToken ct = default)
        {
            try
            {
                return Ok(await _service.DeclineCurrentStageAsync(batchId, GetCurrentUserId(), GetCurrentRoleNames(), request?.Comments, ct));
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

        [HttpPost("{batchId:int}/resubmit")]
        public async Task<ActionResult<WorkflowBatchDetailDto>> Resubmit(int batchId, [FromBody] WorkflowActionRequest? request, CancellationToken ct = default)
        {
            try
            {
                return Ok(await _service.ResubmitReturnedBatchAsync(batchId, GetCurrentUserId(), request?.Comments, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{batchId:int}/cancel")]
        public async Task<ActionResult<WorkflowBatchDetailDto>> Cancel(int batchId, [FromBody] WorkflowActionRequest? request, CancellationToken ct = default)
        {
            try
            {
                return Ok(await _service.CancelReturnedBatchAsync(batchId, GetCurrentUserId(), request?.Comments, ct));
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

        private async Task<bool> CanAccessBatchWorkflowAsync(int batchId, CancellationToken ct, bool authorRequiresReturnedStatus = false)
        {
            var userId = GetCurrentUserId();
            var roleNames = GetCurrentRoleNames();

            if (roleNames.Any(x => string.Equals(x, AppRoles.Admin, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            var canSeeAsAuthor = roleNames.Any(x =>
                    string.Equals(x, AppRoles.Author, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x, AppRoles.WorkflowTrackingUser, StringComparison.OrdinalIgnoreCase))
                && await _service.CanAuthorAccessBatchAsync(userId, batchId, authorRequiresReturnedStatus, ct);

            if (canSeeAsAuthor)
            {
                return true;
            }

            var canSeeAsReviewer = roleNames.Any(x =>
                    string.Equals(x, AppRoles.WorkflowReviewerUodide, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x, AppRoles.WorkflowReviewerAreaTecnica, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x, AppRoles.WorkflowProcessorAreaTecnica, StringComparison.OrdinalIgnoreCase))
                && (await _service.GetReviewInboxAsync(userId, roleNames, 200, ct)).Any(x => x.ImportBatchId == batchId);

            return canSeeAsReviewer;
        }
    }
}
