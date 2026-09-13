using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Unified.Contracts.Administration;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified;

[ApiController]
[Authorize(Roles = "superadmin")]
[Route("api/admin")]
public sealed class UnifiedAdministrativeOperationsController(
    IDataMigrationService dataMigrations,
    IOperationExecutionHistoryService history,
    IUnifiedArticleUserContext userContext) : ControllerBase
{
    [HttpGet("data-migrations")]
    public async Task<ActionResult<ServiceResult<IReadOnlyList<DataMigrationItem>>>> ListDataMigrations(CancellationToken ct)
        => (await dataMigrations.ListAsync(ct)).ToActionResult();

    [HttpPost("data-migrations/{code}/apply")]
    public async Task<ActionResult<ServiceResult<DataMigrationApplyResult>>> ApplyDataMigration(
        string code, [FromHeader(Name = "X-Admin-Operation-Secret")] string? secret, CancellationToken ct)
        => (await dataMigrations.ApplyAsync(code, secret, await userContext.GetAppUserIdAsync(ct),
            userContext.DisplayName, ct)).ToActionResult();

    [HttpGet("operations")]
    public async Task<ActionResult<ServiceResult<OperationExecutionPage>>> ListOperations(
        [FromQuery] OperationExecutionQuery query, CancellationToken ct)
        => ServiceResult<OperationExecutionPage>.Ok(await history.ListAsync(query, ct)).ToActionResult();

    [HttpGet("operations/{executionId:guid}")]
    public async Task<ActionResult<ServiceResult<OperationExecutionItem>>> GetOperation(Guid executionId, CancellationToken ct)
    {
        var item = await history.GetAsync(executionId, ct);
        return item is null
            ? ServiceResult<OperationExecutionItem>.Fail("No se encontró la ejecución.", ErrorType.NotFound,
                ErrorCodes.AdministrativeOperations.OperationExecutionNotFound).ToActionResult()
            : ServiceResult<OperationExecutionItem>.Ok(item).ToActionResult();
    }
}
