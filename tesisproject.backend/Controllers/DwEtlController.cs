using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Analytic.Interfaces;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/analytics/etl")]
    //[Authorize(Roles = "Admin")] // o el rol que tú uses
    public class DwEtlController : ControllerBase
    {
        private readonly IDwEtlService _etlService;

        public DwEtlController(IDwEtlService etlService)
        {
            _etlService = etlService;
        }

        /// <summary>
        /// Ejecuta el ETL completo del DW bajo demanda.
        /// </summary>
        [HttpPost("run-full")]
        public async Task<ActionResult<ApiResponse<NoContent>>> RunFull(CancellationToken ct)
        {
            var result = await _etlService.RunFullLoadAsync(ct);
            return result.ToActionResult();
        }

        // (Opcional) Endpoints separados si quieres lanzar partes específicas:

        [HttpPost("dimensions")]
        public async Task<ActionResult<ApiResponse<NoContent>>> RunDimensions(CancellationToken ct)
            => (await _etlService.LoadDimensionsAsync(ct)).ToActionResult();

        [HttpPost("bridges")]
        public async Task<ActionResult<ApiResponse<NoContent>>> RunBridges(CancellationToken ct)
            => (await _etlService.LoadBridgesAsync(ct)).ToActionResult();

        [HttpPost("facts")]
        public async Task<ActionResult<ApiResponse<NoContent>>> RunFacts(CancellationToken ct)
            => (await _etlService.LoadFactsAsync(ct)).ToActionResult();
    }
}