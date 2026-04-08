using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;
using tesisproject.shared.DTOs.Catalogs;
using tesisproject.shared.DTOs.Configuration;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // Catálogos públicos para el frontend
    public class CatalogsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<CatalogsController> _logger;

        public CatalogsController(AppDbContext db, ILogger<CatalogsController> logger)
        {
            _db = db;
            _logger = logger;
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
            try
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
            catch (SqlException ex) when (ex.Number == 208 && ex.Message.Contains("IndexingSources", System.StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(ex, "La tabla IndexingSources no existe en la base actual. Se devolverá un catálogo vacío.");
                return Ok(new List<CatalogItemDto>());
            }
        }

        // ========== Proyectos ==========
        [HttpGet("projects")]
        public async Task<ActionResult<List<CatalogItemDto>>> GetProjects(CancellationToken ct)
        {
            try
            {
                var list = await _db.Projects
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogItemDto
                    {
                        Id = x.Id,
                        Name = x.Name
                    })
                    .ToListAsync(ct);

                return Ok(list);
            }
            catch (SqlException ex) when (ex.Number == 208 && ex.Message.Contains("Projects", System.StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(ex, "La tabla Projects no existe en la base actual. Se devolverá un catálogo vacío.");
                return Ok(new List<CatalogItemDto>());
            }
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

        [HttpGet("admin/{catalogKey}")]
        public async Task<ActionResult<List<CatalogAdminItemDto>>> GetAdminCatalog(string catalogKey, CancellationToken ct)
        {
            var items = await GetAdminCatalogItemsAsync(catalogKey, ct);
            if (items is null)
            {
                return NotFound();
            }

            return Ok(items);
        }

        [HttpPost("admin/{catalogKey}")]
        public async Task<ActionResult<CatalogAdminItemDto>> CreateAdminCatalogItem(
            string catalogKey,
            [FromBody] UpsertCatalogItemRequest request,
            CancellationToken ct)
        {
            try
            {
                var item = await CreateAdminCatalogItemAsync(catalogKey, request, ct);
                return Ok(item);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("admin/{catalogKey}/{id:int}")]
        public async Task<ActionResult<CatalogAdminItemDto>> UpdateAdminCatalogItem(
            string catalogKey,
            int id,
            [FromBody] UpsertCatalogItemRequest request,
            CancellationToken ct)
        {
            try
            {
                var item = await UpdateAdminCatalogItemAsync(catalogKey, id, request, ct);
                if (item is null)
                {
                    return NotFound();
                }

                return Ok(item);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private async Task<List<CatalogAdminItemDto>?> GetAdminCatalogItemsAsync(string catalogKey, CancellationToken ct)
        {
            return NormalizeCatalogKey(catalogKey) switch
            {
                "academic-terms" => await _db.AcademicTerms
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogAdminItemDto { Id = x.AcademicTermId, Name = x.Name })
                    .ToListAsync(ct),

                "research-lines" => await _db.ResearchLines
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogAdminItemDto { Id = x.ResearchLineId, Name = x.Name })
                    .ToListAsync(ct),

                "broad-fields" => await _db.BroadFields
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogAdminItemDto { Id = x.BroadFieldId, Name = x.Name })
                    .ToListAsync(ct),

                "specific-fields" => await _db.SpecificFields
                    .Include(x => x.BroadField)
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogAdminItemDto
                    {
                        Id = x.SpecificFieldId,
                        Name = x.Name,
                        ParentId = x.BroadFieldId,
                        ParentName = x.BroadField.Name,
                        Code = x.Code
                    })
                    .ToListAsync(ct),

                "detailed-fields" => await _db.DetailedFields
                    .Include(x => x.SpecificField)
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogAdminItemDto
                    {
                        Id = x.DetailedFieldId,
                        Name = x.Name,
                        ParentId = x.SpecificFieldId,
                        ParentName = x.SpecificField.Name,
                        Code = x.Code
                    })
                    .ToListAsync(ct),

                "venues" => await _db.Venues
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogAdminItemDto
                    {
                        Id = x.VenueId,
                        Name = x.Name,
                        Type = x.Type,
                        IssnCode = x.IssnCode,
                        JournalUrl = x.JournalUrl
                    })
                    .ToListAsync(ct),

                _ => null
            };
        }

        private async Task<CatalogAdminItemDto> CreateAdminCatalogItemAsync(string catalogKey, UpsertCatalogItemRequest request, CancellationToken ct)
        {
            var normalizedKey = NormalizeCatalogKey(catalogKey);
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new InvalidOperationException("El nombre es obligatorio.");
            }

            switch (normalizedKey)
            {
                case "academic-terms":
                    var academicTerm = new AcademicTerm { Name = request.Name.Trim() };
                    _db.AcademicTerms.Add(academicTerm);
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = academicTerm.AcademicTermId, Name = academicTerm.Name };

                case "research-lines":
                    var researchLine = new ResearchLine { Name = request.Name.Trim() };
                    _db.ResearchLines.Add(researchLine);
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = researchLine.ResearchLineId, Name = researchLine.Name };

                case "broad-fields":
                    var broadField = new BroadField { Name = request.Name.Trim() };
                    _db.BroadFields.Add(broadField);
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = broadField.BroadFieldId, Name = broadField.Name };

                case "specific-fields":
                    if (!request.ParentId.HasValue) throw new InvalidOperationException("SpecificField requiere ParentId (BroadFieldId).");
                    var specificField = new SpecificField { Name = request.Name.Trim(), BroadFieldId = request.ParentId.Value, Code = NormalizeNullable(request.Code) };
                    _db.SpecificFields.Add(specificField);
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = specificField.SpecificFieldId, Name = specificField.Name, ParentId = specificField.BroadFieldId, Code = specificField.Code };

                case "detailed-fields":
                    if (!request.ParentId.HasValue) throw new InvalidOperationException("DetailedField requiere ParentId (SpecificFieldId).");
                    var detailedField = new DetailedField { Name = request.Name.Trim(), SpecificFieldId = request.ParentId.Value, Code = NormalizeNullable(request.Code) };
                    _db.DetailedFields.Add(detailedField);
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = detailedField.DetailedFieldId, Name = detailedField.Name, ParentId = detailedField.SpecificFieldId, Code = detailedField.Code };

                case "venues":
                    var venue = new Venue
                    {
                        Name = request.Name.Trim(),
                        Type = string.IsNullOrWhiteSpace(request.Type) ? "Journal" : request.Type.Trim(),
                        IssnCode = NormalizeNullable(request.IssnCode),
                        JournalUrl = NormalizeNullable(request.JournalUrl)
                    };
                    _db.Venues.Add(venue);
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = venue.VenueId, Name = venue.Name, Type = venue.Type, IssnCode = venue.IssnCode, JournalUrl = venue.JournalUrl };

                default:
                    throw new InvalidOperationException("Catálogo no soportado.");
            }
        }

        private async Task<CatalogAdminItemDto?> UpdateAdminCatalogItemAsync(string catalogKey, int id, UpsertCatalogItemRequest request, CancellationToken ct)
        {
            var normalizedKey = NormalizeCatalogKey(catalogKey);

            switch (normalizedKey)
            {
                case "academic-terms":
                    var academicTerm = await _db.AcademicTerms.FirstOrDefaultAsync(x => x.AcademicTermId == id, ct);
                    if (academicTerm is null) return null;
                    academicTerm.Name = request.Name.Trim();
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = academicTerm.AcademicTermId, Name = academicTerm.Name };

                case "research-lines":
                    var researchLine = await _db.ResearchLines.FirstOrDefaultAsync(x => x.ResearchLineId == id, ct);
                    if (researchLine is null) return null;
                    researchLine.Name = request.Name.Trim();
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = researchLine.ResearchLineId, Name = researchLine.Name };

                case "broad-fields":
                    var broadField = await _db.BroadFields.FirstOrDefaultAsync(x => x.BroadFieldId == id, ct);
                    if (broadField is null) return null;
                    broadField.Name = request.Name.Trim();
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = broadField.BroadFieldId, Name = broadField.Name };

                case "specific-fields":
                    var specificField = await _db.SpecificFields.FirstOrDefaultAsync(x => x.SpecificFieldId == id, ct);
                    if (specificField is null) return null;
                    specificField.Name = request.Name.Trim();
                    specificField.Code = NormalizeNullable(request.Code);
                    if (request.ParentId.HasValue) specificField.BroadFieldId = request.ParentId.Value;
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = specificField.SpecificFieldId, Name = specificField.Name, ParentId = specificField.BroadFieldId, Code = specificField.Code };

                case "detailed-fields":
                    var detailedField = await _db.DetailedFields.FirstOrDefaultAsync(x => x.DetailedFieldId == id, ct);
                    if (detailedField is null) return null;
                    detailedField.Name = request.Name.Trim();
                    detailedField.Code = NormalizeNullable(request.Code);
                    if (request.ParentId.HasValue) detailedField.SpecificFieldId = request.ParentId.Value;
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = detailedField.DetailedFieldId, Name = detailedField.Name, ParentId = detailedField.SpecificFieldId, Code = detailedField.Code };

                case "venues":
                    var venue = await _db.Venues.FirstOrDefaultAsync(x => x.VenueId == id, ct);
                    if (venue is null) return null;
                    venue.Name = request.Name.Trim();
                    venue.Type = string.IsNullOrWhiteSpace(request.Type) ? venue.Type : request.Type.Trim();
                    venue.IssnCode = NormalizeNullable(request.IssnCode);
                    venue.JournalUrl = NormalizeNullable(request.JournalUrl);
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = venue.VenueId, Name = venue.Name, Type = venue.Type, IssnCode = venue.IssnCode, JournalUrl = venue.JournalUrl };

                default:
                    throw new InvalidOperationException("Catálogo no soportado.");
            }
        }

        private static string NormalizeCatalogKey(string catalogKey)
            => string.IsNullOrWhiteSpace(catalogKey) ? string.Empty : catalogKey.Trim().ToLowerInvariant();

        private static string? NormalizeNullable(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    }
}
