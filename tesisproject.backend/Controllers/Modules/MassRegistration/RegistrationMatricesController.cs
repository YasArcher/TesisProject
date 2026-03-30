using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.MassRegistration;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/registration-matrices")]
    [AllowAnonymous]
    public class RegistrationMatricesController : ControllerBase
    {
        private readonly IRegistrationMatrixService _service;

        public RegistrationMatricesController(IRegistrationMatrixService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<RegistrationMatrixSummaryDto>>> GetMatrices([FromQuery] int take = 50, CancellationToken ct = default)
        {
            try
            {
                return Ok(await _service.GetMatricesAsync(take, ct));
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
                var matrix = await _service.GetMatrixAsync(matrixId, ct);
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
        public async Task<ActionResult<RegistrationMatrixDetailDto>> CreateMatrix([FromBody] CreateRegistrationMatrixRequest request, CancellationToken ct = default)
        {
            try
            {
                return Ok(await _service.CreateMatrixAsync(request, ct));
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
        public async Task<ActionResult<RegistrationMatrixDetailDto>> UpdateMatrix(int matrixId, [FromBody] UpdateRegistrationMatrixRequest request, CancellationToken ct = default)
        {
            var matrix = await _service.UpdateMatrixAsync(matrixId, request, ct);
            return matrix is null ? NotFound() : Ok(matrix);
        }

        [HttpPost("{matrixId:int}/columns")]
        public async Task<ActionResult<RegistrationMatrixDetailDto>> AddColumns(int matrixId, [FromBody] AddRegistrationMatrixColumnsRequest request, CancellationToken ct = default)
        {
            var matrix = await _service.AddColumnsAsync(matrixId, request, ct);
            return matrix is null ? NotFound() : Ok(matrix);
        }

        [HttpPut("{matrixId:int}/columns/{columnId:int}/order")]
        public async Task<ActionResult<RegistrationMatrixDetailDto>> UpdateColumnOrder(int matrixId, int columnId, [FromBody] UpdateRegistrationMatrixColumnOrderRequest request, CancellationToken ct = default)
        {
            var matrix = await _service.UpdateColumnOrderAsync(matrixId, columnId, request, ct);
            return matrix is null ? NotFound() : Ok(matrix);
        }

        [HttpDelete("{matrixId:int}/columns/{columnId:int}")]
        public async Task<IActionResult> RemoveColumn(int matrixId, int columnId, CancellationToken ct = default)
            => await _service.RemoveColumnAsync(matrixId, columnId, ct) ? NoContent() : NotFound();

        [HttpPost("{matrixId:int}/rows")]
        public async Task<ActionResult<RegistrationMatrixDetailDto>> AddRow(int matrixId, CancellationToken ct = default)
        {
            var matrix = await _service.AddRowAsync(matrixId, ct);
            return matrix is null ? NotFound() : Ok(matrix);
        }

        [HttpPut("{matrixId:int}/rows/{rowId:int}/cells")]
        public async Task<ActionResult<RegistrationMatrixDetailDto>> UpdateCell(int matrixId, int rowId, [FromBody] UpdateRegistrationMatrixCellRequest request, CancellationToken ct = default)
        {
            try
            {
                var matrix = await _service.UpdateCellAsync(matrixId, rowId, request, ct);
                return matrix is null ? NotFound() : Ok(matrix);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{matrixId:int}/rows/{rowId:int}")]
        public async Task<IActionResult> DeleteRow(int matrixId, int rowId, CancellationToken ct = default)
            => await _service.DeleteRowAsync(matrixId, rowId, ct) ? NoContent() : NotFound();

        [HttpPost("{matrixId:int}/submit")]
        public async Task<ActionResult<RegistrationMatrixSubmissionResultDto>> SubmitToStaging(int matrixId, [FromBody] SubmitRegistrationMatrixRequest request, CancellationToken ct = default)
        {
            try
            {
                var userId = User?.Identity?.Name ?? "system";
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
    }
}
