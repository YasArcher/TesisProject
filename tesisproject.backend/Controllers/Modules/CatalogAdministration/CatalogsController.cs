using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;
using tesisproject.backend.Identity;
using tesisproject.shared.DTOs.Catalogs;
using tesisproject.shared.DTOs.Configuration;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/scientific-production/catalogs")]
    [Authorize(Policy = AppPolicies.AuthenticatedUser)]
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
            if (!await TableExistsAsync("IndexingSources", ct))
            {
                _logger.LogWarning("La tabla IndexingSources no existe en la base actual. Se devolverá un catálogo vacío.");
                return Ok(new List<CatalogItemDto>());
            }

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

        // ========== Facultades ==========
        [HttpGet("faculties")]
        public async Task<ActionResult<List<CatalogItemDto>>> GetFaculties(CancellationToken ct)
        {
            if (!await TableExistsAsync("Faculties", ct))
            {
                _logger.LogWarning("La tabla Faculties no existe en la base actual. Se devolverá un catálogo vacío.");
                return Ok(new List<CatalogItemDto>());
            }

            var list = await _db.Faculties
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new CatalogItemDto
                {
                    Id = x.FacultyId,
                    Name = x.Name
                })
                .ToListAsync(ct);

            return Ok(list);
        }

        // ========== Proyectos ==========
        [HttpGet("projects")]
        public async Task<ActionResult<List<CatalogItemDto>>> GetProjects(CancellationToken ct)
        {
            if (!await TableExistsAsync("Projects", ct))
            {
                _logger.LogWarning("La tabla Projects no existe en la base actual. Se devolverá un catálogo vacío.");
                return Ok(new List<CatalogItemDto>());
            }

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
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
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
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
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
        [Authorize(Policy = AppPolicies.ConfigurationAdministration)]
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

                "publication-statuses" => await _db.PublicationStatuses
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogAdminItemDto { Id = x.PublicationStatusId, Name = x.Name })
                    .ToListAsync(ct),

                "indexing-sources" => await _db.IndexingSources
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogAdminItemDto { Id = x.IndexingSourceId, Name = x.Name })
                    .ToListAsync(ct),

                "faculties" => await _db.Faculties
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogAdminItemDto { Id = x.FacultyId, Name = x.Name, Code = x.Code })
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

                "projects" => await _db.Projects
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogAdminItemDto
                    {
                        Id = x.Id,
                        Name = x.Name,
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

                case "publication-statuses":
                    var nextPublicationStatusId = await ResolveNextPublicationStatusIdAsync(ct);
                    var publicationStatus = new PublicationStatus { PublicationStatusId = nextPublicationStatusId, Name = request.Name.Trim() };
                    _db.PublicationStatuses.Add(publicationStatus);
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = publicationStatus.PublicationStatusId, Name = publicationStatus.Name };

                case "indexing-sources":
                    var indexingSource = new IndexingSource { Name = request.Name.Trim(), IsActive = true };
                    _db.IndexingSources.Add(indexingSource);
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = indexingSource.IndexingSourceId, Name = indexingSource.Name };

                case "faculties":
                    var faculty = new Faculty { Name = request.Name.Trim(), Code = NormalizeNullable(request.Code) };
                    _db.Faculties.Add(faculty);
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = faculty.FacultyId, Name = faculty.Name, Code = faculty.Code };

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

                case "projects":
                    var project = new Project
                    {
                        Name = request.Name.Trim(),
                        Code = string.IsNullOrWhiteSpace(request.Code) ? GenerateProjectCode(request.Name) : request.Code.Trim()
                    };
                    _db.Projects.Add(project);
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = project.Id, Name = project.Name, Code = project.Code };

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

                case "publication-statuses":
                    var publicationStatus = await _db.PublicationStatuses.FirstOrDefaultAsync(x => x.PublicationStatusId == id, ct);
                    if (publicationStatus is null) return null;
                    publicationStatus.Name = request.Name.Trim();
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = publicationStatus.PublicationStatusId, Name = publicationStatus.Name };

                case "indexing-sources":
                    var indexingSource = await _db.IndexingSources.FirstOrDefaultAsync(x => x.IndexingSourceId == id, ct);
                    if (indexingSource is null) return null;
                    indexingSource.Name = request.Name.Trim();
                    indexingSource.IsActive = true;
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = indexingSource.IndexingSourceId, Name = indexingSource.Name };

                case "faculties":
                    var faculty = await _db.Faculties.FirstOrDefaultAsync(x => x.FacultyId == id, ct);
                    if (faculty is null) return null;
                    faculty.Name = request.Name.Trim();
                    faculty.Code = NormalizeNullable(request.Code);
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = faculty.FacultyId, Name = faculty.Name, Code = faculty.Code };

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

                case "projects":
                    var project = await _db.Projects.FirstOrDefaultAsync(x => x.Id == id, ct);
                    if (project is null) return null;
                    project.Name = request.Name.Trim();
                    project.Code = string.IsNullOrWhiteSpace(request.Code) ? project.Code : request.Code.Trim();
                    await _db.SaveChangesAsync(ct);
                    return new CatalogAdminItemDto { Id = project.Id, Name = project.Name, Code = project.Code };

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

        private async Task<byte> ResolveNextPublicationStatusIdAsync(CancellationToken ct)
        {
            var currentMax = await _db.PublicationStatuses
                .Select(x => (int?)x.PublicationStatusId)
                .MaxAsync(ct) ?? 0;

            if (currentMax >= byte.MaxValue)
            {
                throw new InvalidOperationException("No se pueden crear más estados de publicación porque se alcanzó el límite del catálogo.");
            }

            return (byte)(currentMax + 1);
        }

        private static string GenerateProjectCode(string name)
        {
            var source = string.IsNullOrWhiteSpace(name) ? "PROY" : name.Trim();
            var chars = source
                .Where(char.IsLetterOrDigit)
                .Take(8)
                .ToArray();

            return chars.Length == 0 ? "PROY" : new string(chars).ToUpperInvariant();
        }

        private async Task<bool> TableExistsAsync(string tableName, CancellationToken ct)
        {
            await using var connection = _db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(ct);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 WHERE OBJECT_ID(@tableName, 'U') IS NOT NULL";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@tableName";
            parameter.Value = $"[dbo].[{tableName}]";
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(ct);
            return result is not null && result != DBNull.Value;
        }

    }
}
