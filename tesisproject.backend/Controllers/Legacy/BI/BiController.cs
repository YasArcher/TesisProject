using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.BI.ETL;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/bi")]
    //[Authorize]
    [AllowAnonymous]
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

                // Devolver detalle para debug (luego lo volvemos a simplificar para producción)
                return StatusCode(500, new
                {
                    message = "Error al ejecutar el ETL",
                    error = ex.Message,
                    detail = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}
