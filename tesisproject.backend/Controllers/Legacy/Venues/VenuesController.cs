using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs;
using tesisproject.shared.DTOs.Venues;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/[controller]")]
    //[Authorize] // opcional
    public class VenuesController : ControllerBase
    {
        private readonly IVenuesService _svc;

        public VenuesController(IVenuesService svc) => _svc = svc;

        // ===== Venues =====

        [HttpGet]
        public async Task<ActionResult<PagedResult<VenueDto>>> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            var result = await _svc.GetPagedAsync(page, pageSize, search, ct);
            return Ok(result);
        }

        [HttpGet("{venueId:int}")]
        public async Task<ActionResult<VenueDto>> GetById(int venueId, CancellationToken ct = default)
        {
            var dto = await _svc.GetByIdAsync(venueId, ct);
            if (dto is null) return NotFound();
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<VenueUpsertResponse>> Upsert(
            [FromBody] VenueUpsertRequest req,
            CancellationToken ct = default)
        {
            var resp = await _svc.UpsertVenueAsync(req, ct);
            return Ok(resp);
        }

        [HttpDelete("{venueId:int}")]
        public async Task<ActionResult> Delete(int venueId, CancellationToken ct = default)
        {
            var ok = await _svc.DeleteVenueAsync(venueId, ct);
            return ok ? NoContent() : NotFound();
        }

        // ===== Metrics =====

        [HttpGet("{venueId:int}/metrics")]
        public async Task<ActionResult<IReadOnlyList<VenueMetricDto>>> GetMetrics(int venueId, CancellationToken ct = default)
        {
            var list = await _svc.GetMetricsAsync(venueId, ct);
            return Ok(list);
        }

        [HttpPost("{venueId:int}/metrics")]
        public async Task<ActionResult<VenueMetricUpsertResponse>> UpsertMetric(
            int venueId,
            [FromBody] VenueMetricUpsertRequest req,
            CancellationToken ct = default)
        {
            var resp = await _svc.UpsertMetricAsync(venueId, req, ct);
            return Ok(resp);
        }

        // DELETE: api/venues/10/metrics/2024
        // 👉 sin constraint de tipo; si quieres, usa ":int"
        [HttpDelete("{venueId:int}/metrics/{year}")]
        public async Task<ActionResult> DeleteMetric(int venueId, int year, CancellationToken ct = default)
        {
            // Si tu servicio espera short, castea con validación
            if (year < short.MinValue || year > short.MaxValue)
                return BadRequest("year fuera de rango para short.");

            var ok = await _svc.DeleteMetricAsync(venueId, (short)year, ct);
            return ok ? NoContent() : NotFound();
        }
    }
}
