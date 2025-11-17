using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.shared.DTOs.Catalogs;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // Catálogos públicos para el frontend
    public class CatalogsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CatalogsController(AppDbContext db)
        {
            _db = db;
        }

        // ========== Períodos Académicos ==========
        [HttpGet("academic-terms")]
        public async Task<ActionResult<List<CatalogItemDto>>> GetAcademicTerms(CancellationToken ct)
        {
            var list = await _db.AcademicTerms
                .OrderBy(x => x.Name)
                .Select(x => new CatalogItemDto
                {
                    Id = x.AcademicTermId,
                    Name = x.Name
                })
                .ToListAsync(ct);

            return Ok(list);
        }

        // ========== Líneas de Investigación ==========
        [HttpGet("research-lines")]
        public async Task<ActionResult<List<CatalogItemDto>>> GetResearchLines(CancellationToken ct)
        {
            var list = await _db.ResearchLines
                .OrderBy(x => x.Name)
                .Select(x => new CatalogItemDto
                {
                    Id = x.ResearchLineId,
                    Name = x.Name
                })
                .ToListAsync(ct);

            return Ok(list);
        }

        // ========== Campos Amplios ==========
        [HttpGet("broad-fields")]
        public async Task<ActionResult<List<CatalogItemDto>>> GetBroadFields(CancellationToken ct)
        {
            var list = await _db.BroadFields
                .OrderBy(x => x.Name)
                .Select(x => new CatalogItemDto
                {
                    Id = x.BroadFieldId,
                    Name = x.Name
                })
                .ToListAsync(ct);

            return Ok(list);
        }

        // ========== Campos Específicos por Campo Amplio ==========
        [HttpGet("specific-fields")]
        public async Task<ActionResult<List<CatalogItemDto>>> GetSpecificFields(
            [FromQuery] int? broadFieldId,
            CancellationToken ct)
        {
            var query = _db.SpecificFields.AsQueryable();

            if (broadFieldId.HasValue)
                query = query.Where(x => x.BroadFieldId == broadFieldId.Value);

            var list = await query
                .OrderBy(x => x.Name)
                .Select(x => new CatalogItemDto
                {
                    Id = x.SpecificFieldId,
                    Name = x.Name
                })
                .ToListAsync(ct);

            return Ok(list);
        }

        // ========== Campos Detallados por Campo Específico ==========
        [HttpGet("detailed-fields")]
        public async Task<ActionResult<List<CatalogItemDto>>> GetDetailedFields(
            [FromQuery] int? specificFieldId,
            CancellationToken ct)
        {
            var query = _db.DetailedFields.AsQueryable();

            if (specificFieldId.HasValue)
                query = query.Where(x => x.SpecificFieldId == specificFieldId.Value);

            var list = await query
                .OrderBy(x => x.Name)
                .Select(x => new CatalogItemDto
                {
                    Id = x.DetailedFieldId,
                    Name = x.Name
                })
                .ToListAsync(ct);

            return Ok(list);
        }

        // ========== Estados de Publicación ==========
        [HttpGet("publication-statuses")]
        public async Task<ActionResult<List<CatalogItemDto>>> GetPublicationStatuses(CancellationToken ct)
        {
            var list = await _db.PublicationStatuses
                .OrderBy(x => x.Name)
                .Select(x => new CatalogItemDto
                {
                    Id = x.PublicationStatusId, // tinyint en BD → int en DTO
                    Name = x.Name
                })
                .ToListAsync(ct);

            return Ok(list);
        }

        // ========== Fuentes de Indexación ==========
        [HttpGet("indexing-sources")]
        public async Task<ActionResult<List<CatalogItemDto>>> GetIndexingSources(CancellationToken ct)
        {
            var list = await _db.IndexingSources
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new CatalogItemDto
                {
                    Id = x.IndexingSourceId,
                    Name = x.Name
                })
                .ToListAsync(ct);

            return Ok(list);
        }

        // ========== Proyectos ==========
        [HttpGet("projects")]
        public async Task<ActionResult<List<CatalogItemDto>>> GetProjects(CancellationToken ct)
        {
            var list = await _db.Projects
                .OrderBy(x => x.Name)
                .Select(x => new CatalogItemDto
                {
                    Id = x.Id,       // ✅ PK real de Project
                    Name = x.Name
                })
                .ToListAsync(ct);

            return Ok(list);
        }
        // ========== Revistas / Venues ==========
        [HttpGet("venues")]
        public async Task<ActionResult<List<VenueCatalogItemDto>>> GetVenues(CancellationToken ct)
        {
            var list = await _db.Venues
                .OrderBy(v => v.Name)
                .Select(v => new VenueCatalogItemDto
                {
                    Id = v.VenueId,
                    Name = v.Name,
                    IssnCode = v.IssnCode,
                    JournalUrl = v.JournalUrl
                })
                .ToListAsync(ct);

            return Ok(list);
        }

    }
}
