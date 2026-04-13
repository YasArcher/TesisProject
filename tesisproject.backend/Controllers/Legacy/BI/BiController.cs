using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.BI.ETL;
using tesisproject.backend.Filters;
using tesisproject.backend.Identity;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/bi")]
    [Authorize(Policy = AppPolicies.SecurityAdministration)]
    [ServiceFilter(typeof(LegacyReportingEnabledFilter))]
    public class BiController : ControllerBase
    {
        private readonly IEtlOrchestrator _etl;

        public BiController(IEtlOrchestrator etl)
        {
            _etl = etl;
        }

        [HttpPost("etl/full")]
        public async Task<IActionResult> RunFullEtl(CancellationToken ct)
        {
            try
            {
                await _etl.RunFullLoadAsync(ct);
                return Ok(new { message = "ETL full ejecutado correctamente" });
            }
            catch (Exception ex)
            {
                // Loguear a consola
                Console.Error.WriteLine(ex.ToString());

                return StatusCode(500, new
                {
                    message = "Error al ejecutar el ETL",
                    detail = ex.InnerException?.Message ?? ex.Message
                });
            }
        }
    }
}
