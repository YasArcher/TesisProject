using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using tesisproject.backend.Identity;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.MassRegistration;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/registration-matrices")]
    [Route("api/scientific-production/registration-matrices")]
    [Authorize(Policy = AppPolicies.AuthenticatedUser)]
    public class RegistrationMatricesController : ControllerBase
    {
        private readonly IRegistrationMatrixService _service;
        private readonly IRegistrationWorkflowSettingsService _workflowSettings;
        private readonly UserManager<ApplicationUser> _userManager;

        public RegistrationMatricesController(
            IRegistrationMatrixService service,
            IRegistrationWorkflowSettingsService workflowSettings,
            UserManager<ApplicationUser> userManager)
        {
            _service = service;
            _workflowSettings = workflowSettings;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<List<RegistrationMatrixSummaryDto>>> GetMatrices([FromQuery] int take = 50, CancellationToken ct = default)
        {
            try
            {
                return Ok(await _service.GetMatricesAsync(take, GetCurrentUserId(), CanManageMatrices(), ct));
            }
            catch (SqlException ex) when (ex.Number == 208 && ex.Message.Contains("RegistrationMatrix", System.StringComparison.OrdinalIgnoreCase))
            {
                return Problem(
                    title: "El módulo de matriz maestra todavía no está disponible en la base actual.",
                    detail: "Faltan las tablas del módulo de matriz maestra. Debes aplicar la migración del módulo antes de usar esta pantalla.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("{matrixId:int}")]
        public async Task<ActionResult<RegistrationMatrixDetailDto>> GetMatrix(int matrixId, CancellationToken ct = default)
        {
            try
            {
                var matrix = await _service.GetMatrixAsync(matrixId, GetCurrentUserId(), CanManageMatrices(), ct);
                return matrix is null ? NotFound() : Ok(matrix);
            }
            catch (SqlException ex) when (ex.Number == 208 && ex.Message.Contains("RegistrationMatrix", System.StringComparison.OrdinalIgnoreCase))
            {
                return Problem(
                    title: "No pude abrir la matriz.",
                    detail: "Faltan las tablas del módulo de matriz maestra en la base actual.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Authorize(Policy = AppPolicies.AuthorSubmission)]
        public async Task<ActionResult<RegistrationMatrixDetailDto>> CreateMatrix([FromBody] CreateRegistrationMatrixRequest request, CancellationToken ct = default)
        {
            try
            {
                if (!await CanInitiateRegistrationAsync(ct))
                {
                    return RegistrationNotEnabledProblem();
                }

                if (!await HasAcceptedAuthorTermsAsync())
                {
                    return BadRequest(new { message = "Debes aceptar los términos y condiciones de manejo de información antes de crear matrices de registro." });
                }

                return Ok(await _service.CreateMatrixAsync(request, GetCurrentUserId(), ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (SqlException ex) when (ex.Number == 208 && ex.Message.Contains("RegistrationMatrix", System.StringComparison.OrdinalIgnoreCase))
            {
                return Problem(
                    title: "No pude crear la matriz.",
                    detail: "Faltan las tablas del módulo de matriz maestra. Aplica la migración del módulo para habilitar esta funcionalidad.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut("{matrixId:int}")]
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
        public async Task<ActionResult<RegistrationMatrixDetailDto>> UpdateMatrix(int matrixId, [FromBody] UpdateRegistrationMatrixRequest request, CancellationToken ct = default)
        {
            var matrix = await _service.UpdateMatrixAsync(matrixId, request, ct);
            return matrix is null ? NotFound() : Ok(matrix);
        }

        [HttpPost("{matrixId:int}/columns")]
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
        public async Task<ActionResult<RegistrationMatrixDetailDto>> AddColumns(int matrixId, [FromBody] AddRegistrationMatrixColumnsRequest request, CancellationToken ct = default)
        {
            var matrix = await _service.AddColumnsAsync(matrixId, request, ct);
            return matrix is null ? NotFound() : Ok(matrix);
        }

        [HttpPut("{matrixId:int}/columns/{columnId:int}/order")]
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
        public async Task<ActionResult<RegistrationMatrixDetailDto>> UpdateColumnOrder(int matrixId, int columnId, [FromBody] UpdateRegistrationMatrixColumnOrderRequest request, CancellationToken ct = default)
        {
            var matrix = await _service.UpdateColumnOrderAsync(matrixId, columnId, request, ct);
            return matrix is null ? NotFound() : Ok(matrix);
        }

        [HttpDelete("{matrixId:int}/columns/{columnId:int}")]
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
        public async Task<IActionResult> RemoveColumn(int matrixId, int columnId, CancellationToken ct = default)
            => await _service.RemoveColumnAsync(matrixId, columnId, ct) ? NoContent() : NotFound();

        [HttpPost("{matrixId:int}/rows")]
        [Authorize(Policy = AppPolicies.AuthorSubmission)]
        public async Task<ActionResult<RegistrationMatrixDetailDto>> AddRow(int matrixId, CancellationToken ct = default)
        {
            if (!await CanInitiateRegistrationAsync(ct))
            {
                return RegistrationNotEnabledProblem();
            }

            if (!await CanAccessMatrixAsync(matrixId, ct))
            {
                return NotFound();
            }

            if (!await HasAcceptedAuthorTermsAsync())
            {
                return BadRequest(new { message = "Debes aceptar los términos y condiciones de manejo de información antes de agregar filas." });
            }

            var matrix = await _service.AddRowAsync(matrixId, ct);
            return matrix is null ? NotFound() : Ok(matrix);
        }

        [HttpPut("{matrixId:int}/rows/{rowId:int}/cells")]
        [Authorize(Policy = AppPolicies.AuthorSubmission)]
        public async Task<ActionResult<RegistrationMatrixDetailDto>> UpdateCell(int matrixId, int rowId, [FromBody] UpdateRegistrationMatrixCellRequest request, CancellationToken ct = default)
        {
            try
            {
                if (!await CanInitiateRegistrationAsync(ct))
                {
                    return RegistrationNotEnabledProblem();
                }

                if (!await CanAccessMatrixAsync(matrixId, ct))
                {
                    return NotFound();
                }

                if (!await HasAcceptedAuthorTermsAsync())
                {
                    return BadRequest(new { message = "Debes aceptar los términos y condiciones de manejo de información antes de editar la matriz." });
                }

                var matrix = await _service.UpdateCellAsync(matrixId, rowId, request, ct);
                return matrix is null ? NotFound() : Ok(matrix);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{matrixId:int}/rows/{rowId:int}")]
        [Authorize(Policy = AppPolicies.AuthorSubmission)]
        public async Task<IActionResult> DeleteRow(int matrixId, int rowId, CancellationToken ct = default)
        {
            if (!await CanInitiateRegistrationAsync(ct))
            {
                return RegistrationNotEnabledProblem();
            }

            if (!await CanAccessMatrixAsync(matrixId, ct))
            {
                return NotFound();
            }

            if (!await HasAcceptedAuthorTermsAsync())
            {
                return BadRequest(new { message = "Debes aceptar los términos y condiciones de manejo de información antes de eliminar filas." });
            }

            return await _service.DeleteRowAsync(matrixId, rowId, ct) ? NoContent() : NotFound();
        }

        [HttpPost("{matrixId:int}/submit")]
        [Authorize(Policy = AppPolicies.AuthorSubmission)]
        public async Task<ActionResult<RegistrationMatrixSubmissionResultDto>> SubmitToStaging(int matrixId, [FromBody] SubmitRegistrationMatrixRequest request, CancellationToken ct = default)
        {
            try
            {
                if (!await CanInitiateRegistrationAsync(ct))
                {
                    return RegistrationNotEnabledProblem();
                }

                if (!await CanAccessMatrixAsync(matrixId, ct))
                {
                    return NotFound();
                }

                if (!await HasAcceptedAuthorTermsAsync())
                {
                    return BadRequest(new { message = "Debes aceptar los términos y condiciones de manejo de información antes de enviar la matriz a revisión." });
                }

                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User?.Identity?.Name
                    ?? "system";
                return Ok(await _service.SubmitToStagingAsync(matrixId, request, userId, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Problem(title: "No pude enviar la matriz a staging.", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<bool> CanAccessMatrixAsync(int matrixId, CancellationToken ct)
            => await _service.GetMatrixAsync(matrixId, GetCurrentUserId(), CanManageMatrices(), ct) is not null;

        private bool CanManageMatrices()
            => User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Analyst);

        private Task<bool> CanInitiateRegistrationAsync(CancellationToken ct)
            => _workflowSettings.CanInitiateMatrixRegistrationAsync(User, ct);

        private ObjectResult RegistrationNotEnabledProblem()
            => Problem(
                title: "Registro no habilitado para tu rol.",
                detail: "El modo institucional actual no permite que este perfil inicie o edite matrices de registro.",
                statusCode: StatusCodes.Status403Forbidden);

        private string? GetCurrentUserId()
            => User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User?.Identity?.Name;

        private async Task<bool> HasAcceptedAuthorTermsAsync()
        {
            if (!User.IsInRole(AppRoles.Author) || CanManageMatrices())
            {
                return true;
            }

            var user = await _userManager.GetUserAsync(User);
            return user?.TermsAcceptedAt is not null;
        }
    }
}
