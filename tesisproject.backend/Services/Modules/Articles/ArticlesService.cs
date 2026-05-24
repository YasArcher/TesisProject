using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;
using tesisproject.backend.Mapping;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs;
using tesisproject.shared.DTOs.Articles;
using ClosedXML.Excel;


namespace tesisproject.backend.Services.Implementations
{
    public class ArticlesService : IArticlesService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ArticlesService> _logger;

        public ArticlesService(AppDbContext db, ILogger<ArticlesService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // =========================================================
        // LIST (paginado + filtros)
        // =========================================================
        public async Task<PagedResult<ArticleListItemDto>> GetListAsync(
            ArticleListQuery query,
            CancellationToken ct = default)
        {
            var q = _db.Articles
                .AsNoTracking()
                .Include(a => a.Venue)
                .Include(a => a.PublicationStatus)
                .Include(a => a.Faculty)
                .OrderByDescending(a => a.Year)
                .ThenBy(a => a.Title)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
                q = q.Where(a => a.Title!.Contains(s) || a.Doi == s);
            }

            if (query.Year.HasValue)
                q = q.Where(a => a.Year == query.Year);

            var total = await q.CountAsync(ct);
            var items = await q.Skip((query.Page - 1) * query.PageSize)
                               .Take(query.PageSize)
                               .Select(a => a.ToListItemDto())
                               .ToListAsync(ct);

            return new PagedResult<ArticleListItemDto>(items, total, query.Page, query.PageSize);
        }

        // =========================================================
        // EXPORT BI (lista completa desnormalizada)
        // =========================================================
        public async Task<List<ArticleBiExportDto>> GetBiExportAsync(
     ArticleListQuery query,
     CancellationToken ct = default)
        {
            // ⏱️ Aumentamos el timeout SOLO para esta operación "pesada"
            _db.Database.SetCommandTimeout(120);

            var q = _db.Articles
                .AsNoTracking()
                // 🔹 Evita el "join monster": cada Include de colección se hace en query separada
                .AsSplitQuery()
                .Include(a => a.Venue)!.ThenInclude(v => v!.VenueMetrics)
                .Include(a => a.AcademicTerm)
                .Include(a => a.PublicationStatus)
                .Include(a => a.ResearchLine)
                .Include(a => a.Faculty)
                .Include(a => a.BroadField)
                .Include(a => a.SpecificField)
                .Include(a => a.DetailedField)
                .Include(a => a.Indexings)!.ThenInclude(ix => ix.IndexingSource)
                .Include(a => a.Participants)
                .AsQueryable();

            // 🔎 Filtros básicos reutilizando lo que ya tienes
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
                q = q.Where(a => a.Title!.Contains(s) || a.Doi == s);
            }

            if (query.Year.HasValue)
                q = q.Where(a => a.Year == query.Year);

            // Puedes añadir más filtros con query.PublicationStatusId, etc. si luego lo necesitas

            var list = await q
                .OrderByDescending(a => a.Year)
                .ThenBy(a => a.Title)
                .ToListAsync(ct);

            var result = new List<ArticleBiExportDto>(list.Count);

            foreach (var a in list)
            {
                // ===== Métrica SJR / Cuartil (igual lógica que en GetByIdAsync) =====
                decimal? sjr = null;
                string? quartile = null;

                if (a.Venue?.VenueMetrics != null && a.Venue.VenueMetrics.Count > 0)
                {
                    var metric = a.Venue.VenueMetrics
                        .OrderByDescending(m => m.Year == a.Year) // primero las que coinciden con el año del artículo
                        .ThenByDescending(m => m.Year)
                        .FirstOrDefault();

                    if (metric != null)
                    {
                        sjr = metric.SJR;
                        quartile = metric.Quartile;
                    }
                }

                // ===== Fuentes de indexación =====
                var indexingNames = a.Indexings?
                    .Where(ix => ix.IndexingSource != null)
                    .Select(ix => ix.IndexingSource!.Name)
                    .Distinct()
                    .ToList() ?? new List<string>();

                // ===== Autores =====
                var authorNames = a.Participants?
                    .OrderBy(p => p.Index)
                    .Select(p => p.Nombre)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToList() ?? new List<string>();

                var dto = new ArticleBiExportDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Doi = a.Doi,
                    Year = a.Year,
                    PublishedAt = a.PublishedAt,
                    PageCount = a.PageCount,
                    PublicationUrl = a.PublicationUrl,

                    VenueName = a.Venue?.Name,
                    IssnCode = a.Venue?.IssnCode,
                    IssueNumber = a.Venue?.IssueNumber,
                    VolumeNumber = a.Venue?.VolumeNumber,
                    JournalUrl = a.Venue?.JournalUrl,

                    AcademicTermName = a.AcademicTerm?.Name,
                    PublicationStatusName = a.PublicationStatus?.Name,
                    ResearchLineName = a.ResearchLine?.Name,
                    BroadFieldName = a.BroadField?.Name,
                    SpecificFieldName = a.SpecificField?.Name,
                    DetailedFieldName = a.DetailedField?.Name,

                    IsProjectResult = a.IsProjectResult,
                    HasInterculturalComponent = a.HasInterculturalComponent,
                    IsOpenAccess = a.IsOpenAccess,

                    ProceedingsName = a.ProceedingsName,
                    Proceedings = a.Proceedings,
                    EventName = a.EventName,
                    GroupName = a.GroupName,
                    Filiacion = a.Filiacion,

                    Sjr = sjr,
                    Quartile = quartile,

                    IndexingSources = indexingNames.Count > 0
                        ? string.Join(" | ", indexingNames)
                        : null,
                    IndexingSourcesCount = indexingNames.Count,

                    Authors = authorNames.Count > 0
                        ? string.Join(" | ", authorNames)
                        : null,
                    AuthorsCount = authorNames.Count
                };

                result.Add(dto);
            }

            return result;
        }

        public async Task<byte[]> ExportBiCsvAsync(CancellationToken ct = default)
        {
            // Exportamos TODOS los artículos (puedes luego añadir filtros si quieres)
            var query = new ArticleListQuery
            {
                Page = 1,
                PageSize = int.MaxValue
            };

            var items = await GetBiExportAsync(query, ct);

            var csv = BuildBiCsv(items);
            return Encoding.UTF8.GetBytes(csv);
        }

        private static string BuildBiCsv(IEnumerable<ArticleBiExportDto> items)
        {
            var sb = new StringBuilder();

            // Encabezado (mismo orden de columnas que usa ImportBiAsync)
            sb.AppendLine(string.Join(";", new[]
            {
        // 0–6: datos del artículo
        "ArticuloId",                 // Id interno del artículo
        "TituloArticulo",             // Título del artículo
        "Doi",                        // DOI
        "AnioPublicacion",            // Año (numérico)
        "FechaPublicacion",           // Fecha exacta de publicación (yyyy-MM-dd)
        "NumeroPaginas",              // Número de páginas
        "UrlPublicacion",             // URL donde está publicado el artículo

        // 7–11: datos de la revista / venue
        "RevistaNombre",              // Nombre de la revista o medio
        "RevistaIssn",                // ISSN
        "RevistaNumero",              // Número / Issue
        "RevistaVolumen",             // Volumen
        "RevistaUrl",                 // URL de la revista

        // 12–18: clasificación académica / OCDE / proyecto
        "PeriodoAcademicoNombre",     // Nombre del período académico
        "EstadoPublicacionNombre",    // Estado de publicación (Aceptado, Publicado, etc.)
        "LineaInvestigacionNombre",   // Línea de investigación
        "CampoAmplioOCDE",            // Campo amplio OCDE
        "CampoEspecificoOCDE",        // Campo específico OCDE
        "CampoDetalladoOCDE",         // Campo detallado OCDE
        "ProyectoNombre",             // Nombre del proyecto (si aplica)

        // 19–21: flags lógicos
        "EsResultadoDeProyecto",      // Si el artículo es resultado de un proyecto
        "TieneComponenteIntercultural", // Si tiene componente intercultural
        "EsAccesoAbierto",            // Open Access

        // 22–26: evento / grupo / filiación
        "MemoriasNombre",             // Nombre de memorias
        "MemoriasDetalle",            // Detalle de memorias
        "EventoNombre",               // Nombre del evento
        "GrupoArticulado",            // Grupo articulado / grupo de investigación
        "Filiacion",                  // Filiación institucional

        // 27–28: métricas de revista
        "Sjr",                        // SJR (numérico)
        "Cuartil",                    // Cuartil (Q1, Q2, etc.)

        // 29–32: indexación y autores
        "FuentesIndexacion",          // Nombres de bases de datos, separados con " | "
        "NumeroFuentesIndexacion",    // Conteo de fuentes de indexación
        "Autores",                    // Nombres de autores, separados con " | "
        "NumeroAutores"               // Conteo de autores
    }));

            foreach (var a in items)
            {
                // Formateador base
                string F(object? v) => v switch
                {
                    null => "",
                    bool b => b ? "Si" : "No",
                    DateTime dt => dt.ToString("yyyy-MM-dd"),
                    decimal dec => dec.ToString(CultureInfo.InvariantCulture),
                    double dbl => dbl.ToString(CultureInfo.InvariantCulture),
                    float fl => fl.ToString(CultureInfo.InvariantCulture),
                    _ => v?.ToString() ?? ""
                };

                // Aplica comillas si hace falta (para ;, saltos de línea, comillas, etc.)
                string Q(object? v)
                {
                    var s = F(v);
                    if (s.Contains(';') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
                    {
                        s = s.Replace("\"", "\"\"");
                        return $"\"{s}\"";
                    }
                    return s;
                }

                sb.AppendLine(string.Join(";", new[]
                {
            // 0–6: datos del artículo
            F(a.Id),
            Q(a.Title),
            Q(a.Doi),
            F(a.Year),
            F(a.PublishedAt),
            F(a.PageCount),
            Q(a.PublicationUrl),

            // 7–11: revista / venue
            Q(a.VenueName),
            Q(a.IssnCode),
            Q(a.IssueNumber),
            Q(a.VolumeNumber),
            Q(a.JournalUrl),

            // 12–18: académico / OCDE / proyecto
            Q(a.AcademicTermName),
            Q(a.PublicationStatusName),
            Q(a.ResearchLineName),
            Q(a.BroadFieldName),
            Q(a.SpecificFieldName),
            Q(a.DetailedFieldName),
            Q(a.ProjectName),

            // 19–21: flags
            F(a.IsProjectResult),
            F(a.HasInterculturalComponent),
            F(a.IsOpenAccess),

            // 22–26: evento / grupo / filiación
            Q(a.ProceedingsName),
            Q(a.Proceedings),
            Q(a.EventName),
            Q(a.GroupName),
            Q(a.Filiacion),

            // 27–28: métricas
            F(a.Sjr),
            Q(a.Quartile),

            // 29–32: indexación y autores
            Q(a.IndexingSources),
            F(a.IndexingSourcesCount),
            Q(a.Authors),
            F(a.AuthorsCount)
        }));
            }

            return sb.ToString();
        }
        public async Task<byte[]> ExportBiExcelAsync(CancellationToken ct = default)
        {
            var query = new ArticleListQuery
            {
                Page = 1,
                PageSize = int.MaxValue
            };

            var items = await GetBiExportAsync(query, ct);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Artículos BI");

            // Encabezados (coherentes con el CSV, pero no afectan la importación)
            var headers = new[]
            {
        "ArticuloId",
        "TituloArticulo",
        "Doi",
        "AnioPublicacion",
        "FechaPublicacion",
        "NumeroPaginas",
        "UrlPublicacion",

        "RevistaNombre",
        "RevistaIssn",
        "RevistaNumero",
        "RevistaVolumen",
        "RevistaUrl",

        "PeriodoAcademicoNombre",
        "EstadoPublicacionNombre",
        "LineaInvestigacionNombre",
        "CampoAmplioOCDE",
        "CampoEspecificoOCDE",
        "CampoDetalladoOCDE",
        "ProyectoNombre",

        "EsResultadoDeProyecto",
        "TieneComponenteIntercultural",
        "EsAccesoAbierto",

        "MemoriasNombre",
        "MemoriasDetalle",
        "EventoNombre",
        "GrupoArticulado",
        "Filiacion",

        "Sjr",
        "Cuartil",

        "FuentesIndexacion",
        "NumeroFuentesIndexacion",
        "Autores",
        "NumeroAutores"
    };

            // Escribimos encabezados
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
            }

            // Filas de datos
            var row = 2;
            foreach (var a in items)
            {
                ws.Cell(row, 1).Value = a.Id;
                ws.Cell(row, 2).Value = a.Title ?? "";
                ws.Cell(row, 3).Value = a.Doi ?? "";
                ws.Cell(row, 4).Value = a.Year;
                ws.Cell(row, 5).Value = a.PublishedAt?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(row, 6).Value = a.PageCount;
                ws.Cell(row, 7).Value = a.PublicationUrl ?? "";

                ws.Cell(row, 8).Value = a.VenueName ?? "";
                ws.Cell(row, 9).Value = a.IssnCode ?? "";
                ws.Cell(row, 10).Value = a.IssueNumber ?? "";
                ws.Cell(row, 11).Value = a.VolumeNumber ?? "";
                ws.Cell(row, 12).Value = a.JournalUrl ?? "";

                ws.Cell(row, 13).Value = a.AcademicTermName ?? "";
                ws.Cell(row, 14).Value = a.PublicationStatusName ?? "";
                ws.Cell(row, 15).Value = a.ResearchLineName ?? "";
                ws.Cell(row, 16).Value = a.BroadFieldName ?? "";
                ws.Cell(row, 17).Value = a.SpecificFieldName ?? "";
                ws.Cell(row, 18).Value = a.DetailedFieldName ?? "";
                ws.Cell(row, 19).Value = a.ProjectName ?? "";

                ws.Cell(row, 20).Value = a.IsProjectResult ? "Si" : "No";
                ws.Cell(row, 21).Value = a.HasInterculturalComponent ? "Si" : "No";
                ws.Cell(row, 22).Value = a.IsOpenAccess ? "Si" : "No";

                ws.Cell(row, 23).Value = a.ProceedingsName ?? "";
                ws.Cell(row, 24).Value = a.Proceedings ?? "";
                ws.Cell(row, 25).Value = a.EventName ?? "";
                ws.Cell(row, 26).Value = a.GroupName ?? "";
                ws.Cell(row, 27).Value = a.Filiacion ?? "";

                ws.Cell(row, 28).Value = a.Sjr;
                ws.Cell(row, 29).Value = a.Quartile ?? "";

                ws.Cell(row, 30).Value = a.IndexingSources ?? "";
                ws.Cell(row, 31).Value = a.IndexingSourcesCount;
                ws.Cell(row, 32).Value = a.Authors ?? "";
                ws.Cell(row, 33).Value = a.AuthorsCount;

                row++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }


        // =========================================================
        // IMPORT BI (desde CSV desnormalizado)
        // =========================================================
        public async Task<ArticleImportResultDto> ImportBiAsync(
            Stream csvStream,
            string userId,
            CancellationToken ct = default)
        {
            if (csvStream is null) throw new ArgumentNullException(nameof(csvStream));

            var result = new ArticleImportResultDto();

            // 1) Pre-cargar catálogos en memoria (diccionarios por nombre)
            var academicTermsByName = await _db.AcademicTerms
                .AsNoTracking()
                .ToDictionaryAsync(t => NormalizeName(t.Name), t => t.AcademicTermId, ct);

            var statusesByName = await _db.PublicationStatuses
                .AsNoTracking()
                .ToDictionaryAsync(s => NormalizeName(s.Name), s => s.PublicationStatusId, ct);

            var researchLinesByName = await _db.ResearchLines
                .AsNoTracking()
                .ToDictionaryAsync(r => NormalizeName(r.Name), r => r.ResearchLineId, ct);

            var broadFieldsByName = await _db.BroadFields
                .AsNoTracking()
                .ToDictionaryAsync(b => NormalizeName(b.Name), b => b.BroadFieldId, ct);

            var specificFieldsByName = await _db.SpecificFields
                .AsNoTracking()
                .ToDictionaryAsync(s => NormalizeName(s.Name), s => s.SpecificFieldId, ct);

            var detailedFieldsByName = await _db.DetailedFields
                .AsNoTracking()
                .ToDictionaryAsync(d => NormalizeName(d.Name), d => d.DetailedFieldId, ct);

            // 2) Leer CSV
            using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

            string? line;
            int lineNumber = 0;

            // Encabezado (lo saltamos, pero asumimos que sigue el orden definido)
            line = await reader.ReadLineAsync();
            lineNumber++;
            if (line is null)
                return result;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                result.TotalRows++;

                try
                {
                    var cols = SplitCsvLine(line);
                    // Debe coincidir con el orden de columnas del export
                    string Get(int index) =>
                        index < cols.Count ? cols[index]?.Trim() ?? string.Empty : string.Empty;

                    // ---- Mapeo de columnas (mismo orden que en BuildBiCsv) ----
                    var idStr = Get(0);
                    var title = Get(1);
                    var doi = Get(2);
                    var yearStr = Get(3);
                    var publishedAtStr = Get(4);
                    var pageCountStr = Get(5);
                    var publicationUrl = Get(6);

                    var venueName = Get(7);
                    var issn = Get(8);
                    var issueNumber = Get(9);
                    var volumeNumber = Get(10);
                    var journalUrl = Get(11);

                    var academicTermName = Get(12);
                    var statusName = Get(13);
                    var researchLineName = Get(14);
                    var broadFieldName = Get(15);
                    var specificFieldName = Get(16);
                    var detailedFieldName = Get(17);
                    var projectName = Get(18);

                    var isProjectResultStr = Get(19);
                    var hasInterculturalStr = Get(20);
                    var isOpenAccessStr = Get(21);

                    var proceedingsName = Get(22);
                    var proceedings = Get(23);
                    var eventName = Get(24);
                    var groupName = Get(25);
                    var filiacion = Get(26);

                    var sjrStr = Get(27);
                    var quartile = Get(28);
                    // var indexingSources         = Get(29); // Por ahora no modificamos indexaciones
                    // var indexingCountStr        = Get(30);
                    // var authors                 = Get(31); // Por ahora no tocamos autores
                    // var authorsCountStr         = Get(32);

                    // ---- Parse básicos ----
                    int.TryParse(idStr, out var id);
                    short? year = short.TryParse(yearStr, out var tmpYear) ? tmpYear : (short?)null;

                    DateTime? publishedAt = null;
                    if (!string.IsNullOrWhiteSpace(publishedAtStr) &&
                        DateTime.TryParse(publishedAtStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tmpDate))
                    {
                        publishedAt = tmpDate;
                    }

                    int? pageCount = int.TryParse(pageCountStr, out var tmpPages) ? tmpPages : (int?)null;

                    decimal? sjr = null;
                    if (!string.IsNullOrWhiteSpace(sjrStr) &&
                        decimal.TryParse(sjrStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var tmpSjr))
                    {
                        sjr = tmpSjr;
                    }

                    bool isProjectResult = ParseBool(isProjectResultStr);
                    bool hasIntercultural = ParseBool(hasInterculturalStr);
                    bool isOpenAccess = ParseBool(isOpenAccessStr);

                    // ---- Resolver catálogos (por nombre) ----
                    int? academicTermId = null;
                    byte? publicationStatusId = null;
                    int? researchLineId = null;
                    int? broadFieldId = null;
                    int? specificFieldId = null;
                    int? detailedFieldId = null;

                    if (!string.IsNullOrWhiteSpace(academicTermName))
                    {
                        if (!academicTermsByName.TryGetValue(NormalizeName(academicTermName), out var termId))
                            throw new Exception($"Período académico '{academicTermName}' no encontrado.");
                        academicTermId = termId;
                    }

                    if (!string.IsNullOrWhiteSpace(statusName))
                    {
                        if (!statusesByName.TryGetValue(NormalizeName(statusName), out var stId))
                            throw new Exception($"Estado de publicación '{statusName}' no encontrado.");
                        publicationStatusId = stId;
                    }

                    if (!string.IsNullOrWhiteSpace(researchLineName))
                    {
                        if (!researchLinesByName.TryGetValue(NormalizeName(researchLineName), out var rlId))
                            throw new Exception($"Línea de investigación '{researchLineName}' no encontrada.");
                        researchLineId = rlId;
                    }

                    if (!string.IsNullOrWhiteSpace(broadFieldName))
                    {
                        if (!broadFieldsByName.TryGetValue(NormalizeName(broadFieldName), out var bfId))
                            throw new Exception($"Campo amplio '{broadFieldName}' no encontrado.");
                        broadFieldId = bfId;
                    }

                    if (!string.IsNullOrWhiteSpace(specificFieldName))
                    {
                        if (!specificFieldsByName.TryGetValue(NormalizeName(specificFieldName), out var sfId))
                            throw new Exception($"Campo específico '{specificFieldName}' no encontrado.");
                        specificFieldId = sfId;
                    }

                    if (!string.IsNullOrWhiteSpace(detailedFieldName))
                    {
                        if (!detailedFieldsByName.TryGetValue(NormalizeName(detailedFieldName), out var dfId))
                            throw new Exception($"Campo detallado '{detailedFieldName}' no encontrado.");
                        detailedFieldId = dfId;
                    }

                    // ---- Upsert de Venue + métrica SJR (reutilizando helpers existentes) ----
                    var venueRequest = new CreateArticleRequest
                    {
                        JournalName = venueName,
                        IssnCode = issn,
                        IssueNumber = issueNumber,
                        VolumeNumber = volumeNumber,
                        JournalUrl = journalUrl,
                        Year = year,
                        Sjr = sjr,
                        Quartile = string.IsNullOrWhiteSpace(quartile) ? null : quartile
                    };

                    var (venueId, _) = await UpsertVenueAsync(venueRequest, ct);
                    await UpsertVenueMetricAsync(venueId, year, sjr, venueRequest.Quartile, ct);

                    // ---- Crear o actualizar Article ----
                    Article? article = null;
                    var isNew = id <= 0;

                    if (!isNew)
                    {
                        article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == id, ct);
                        if (article == null)
                            throw new Exception($"No existe un artículo con Id {id}.");
                    }

                    if (article == null)
                    {
                        article = new Article
                        {
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.Articles.Add(article);
                        isNew = true;
                    }

                    // Campos básicos
                    article.Title = string.IsNullOrWhiteSpace(title)
                    ? (article.Title ?? throw new Exception("El título del artículo es obligatorio para nuevas filas."))
                    : title.Trim();
                    article.Doi = string.IsNullOrWhiteSpace(doi) ? null : doi.Trim();
                    article.Year = year;
                    article.PublishedAt = publishedAt;
                    article.PageCount = pageCount;
                    article.PublicationUrl = string.IsNullOrWhiteSpace(publicationUrl) ? null : publicationUrl.Trim();

                    article.IsProjectResult = isProjectResult;
                    article.HasInterculturalComponent = hasIntercultural;
                    article.IsOpenAccess = isOpenAccess;

                    article.ProceedingsName = proceedingsName;
                    article.Proceedings = proceedings;
                    article.EventName = eventName;
                    article.GroupName = groupName;
                    article.Filiacion = filiacion;

                    article.AcademicTermId = academicTermId;
                    article.PublicationStatusId = publicationStatusId;
                    article.ResearchLineId = researchLineId;
                    article.BroadFieldId = broadFieldId;
                    article.SpecificFieldId = specificFieldId;
                    article.DetailedFieldId = detailedFieldId;

                    article.VenueId = venueId;

                    // Nota: NO tocamos Participants ni Indexings aquí
                    // para no borrar autores ni fuentes existentes.

                    await _db.SaveChangesAsync(ct);

                    result.Processed++;
                    if (isNew) result.Created++;
                    else result.Updated++;
                }
                catch (Exception ex)
                {
                    result.Errors++;

                    var msg = ex.Message;

                    if (ex is Microsoft.EntityFrameworkCore.DbUpdateException dbEx &&
                        dbEx.InnerException != null)
                    {
                        msg += " | Detalle BD: " + dbEx.InnerException.Message;
                    }

                    result.ErrorMessages.Add($"Línea {lineNumber}: {msg}");
                }

            }

            return result;
        }

        // =========================================================
        // DETAIL
        // =========================================================
        public async Task<ArticleDetailDto?> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            var entity = await _db.Articles
                .Include(a => a.Venue)!.ThenInclude(v => v!.VenueMetrics)
                .Include(a => a.Indexings)!.ThenInclude(ix => ix.IndexingSource)
                .Include(a => a.Faculty)
                .Include(a => a.Participants)
                .Include(a => a.Files)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (entity is null) return null;

            var dto = entity.ToDetailDto();

            // Proyecta SJR/Quartile desde VenueMetrics:
            // 1) preferimos año del artículo; 2) si no hay, la más reciente
            if (entity.Venue?.VenueMetrics?.Count > 0)
            {
                var metric = entity.Venue.VenueMetrics
                    .OrderByDescending(m => m.Year == entity.Year) // true primero
                    .ThenByDescending(m => m.Year)
                    .FirstOrDefault();

                if (metric != null)
                {
                    dto.Sjr = metric.SJR;
                    dto.Quartile = metric.Quartile;
                }
            }

            return dto;
        }

        // =========================================================
        // CREATE
        // =========================================================
        public async Task<int> CreateAsync(
            CreateArticleRequest request,
            string userId,
            CancellationToken ct = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            // 1) Venue & Metric
            var (venueId, _) = await UpsertVenueAsync(request, ct);
            await UpsertVenueMetricAsync(venueId, request.Year, request.Sjr, request.Quartile, ct);

            // 2) Article
            var article = new Article
            {
                Title = request.Title?.Trim(),
                Doi = string.IsNullOrWhiteSpace(request.Doi) ? null : request.Doi.Trim(),
                Year = request.Year,
                PublishedAt = request.PublishedAt,
                PageCount = request.PageCount,
                PublicationUrl = request.PublicationUrl,

                IsProjectResult = request.IsProjectResult,
                HasInterculturalComponent = request.HasInterculturalComponent,
                IsOpenAccess = request.IsOpenAccess,

                ProceedingsName = request.ProceedingsName,
                Proceedings = request.Proceedings,
                EventName = request.EventName,
                GroupName = request.GroupName,
                Filiacion = request.Filiacion,

                AcademicTermId = request.AcademicTermId,
                PublicationStatusId = request.PublicationStatusId,
                ResearchLineId = request.ResearchLineId,
                BroadFieldId = request.BroadFieldId,
                SpecificFieldId = request.SpecificFieldId,
                DetailedFieldId = request.DetailedFieldId,
                FacultyId = request.FacultyId,

                VenueId = venueId
                // Si tus entidades tienen audit fields, agrégalos aquí.
            };

            _db.Articles.Add(article);
            await _db.SaveChangesAsync(ct);

            // 3) Indexings
            await ReplaceIndexingsAsync(article.Id, request.IndexingSourceIds ?? new List<int>(), ct);

            // 4) Participants
            await ReplaceParticipantsAsync(article.Id, request.Participants ?? new List<ArticleParticipantRequest>(), ct);

            // 5) Evidence (ArticleFile simple por URL)
            await UpsertEvidenceAsync(article.Id, request.EvidenceUrl, /*userId*/ null, ct);

            return article.Id;
        }

        // =========================================================
        // UPDATE
        // =========================================================
        public async Task<bool> UpdateAsync(
            int id,
            UpdateArticleRequest request,
            string userId,
            CancellationToken ct = default)
        {
            var article = await _db.Articles
                .Include(a => a.Participants)
                .Include(a => a.Indexings)
                .Include(a => a.Files)
                .Include(a => a.Venue)!.ThenInclude(v => v!.VenueMetrics)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (article is null) return false;

            // 1) Venue & Metric
            var (venueId, _) = await UpsertVenueAsync(request, ct);
            await UpsertVenueMetricAsync(venueId, request.Year, request.Sjr, request.Quartile, ct);

            // 2) Article
            article.Title = request.Title?.Trim();
            article.Doi = string.IsNullOrWhiteSpace(request.Doi) ? null : request.Doi.Trim();
            article.Year = request.Year;
            article.PublishedAt = request.PublishedAt;
            article.PageCount = request.PageCount;
            article.PublicationUrl = request.PublicationUrl;

            article.IsProjectResult = request.IsProjectResult;
            article.HasInterculturalComponent = request.HasInterculturalComponent;
            article.IsOpenAccess = request.IsOpenAccess;

            article.ProceedingsName = request.ProceedingsName;
            article.Proceedings = request.Proceedings;
            article.EventName = request.EventName;
            article.GroupName = request.GroupName;
            article.Filiacion = request.Filiacion;

            article.AcademicTermId = request.AcademicTermId;
            article.PublicationStatusId = request.PublicationStatusId;
            article.ResearchLineId = request.ResearchLineId;
            article.BroadFieldId = request.BroadFieldId;
            article.SpecificFieldId = request.SpecificFieldId;
            article.DetailedFieldId = request.DetailedFieldId;
            article.FacultyId = request.FacultyId;

            article.VenueId = venueId;

            await _db.SaveChangesAsync(ct);

            // 3) Indexings
            await ReplaceIndexingsAsync(article.Id, request.IndexingSourceIds ?? new List<int>(), ct);

            // 4) Participants
            await ReplaceParticipantsAsync(article.Id, request.Participants ?? new List<ArticleParticipantRequest>(), ct);

            // 5) Evidence
            await UpsertEvidenceAsync(article.Id, request.EvidenceUrl, /*userId*/ null, ct);

            return true;
        }

        // =========================================================
        // DELETE
        // =========================================================
        public async Task<bool> DeleteAsync(
            int id,
            string userId,
            CancellationToken ct = default)
        {
            var entity = await _db.Articles
                .Include(a => a.Participants)
                .Include(a => a.Indexings)
                .Include(a => a.Files)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (entity is null) return false;

            // Si tu esquema no tiene cascade, elimina hijos
            if (entity.Participants?.Count > 0)
                _db.ArticleParticipants.RemoveRange(entity.Participants);
            if (entity.Indexings?.Count > 0)
                _db.ArticleIndexings.RemoveRange(entity.Indexings);
            if (entity.Files?.Count > 0)
                _db.ArticleFiles.RemoveRange(entity.Files);

            _db.Articles.Remove(entity);
            await _db.SaveChangesAsync(ct);

            return true;
        }

        // =========================================================
        // Helpers privados
        // =========================================================

        private async Task<(int venueId, bool created)> UpsertVenueAsync(
            CreateArticleRequest r,
            CancellationToken ct)
        {
            var name = (r.JournalName ?? "").Trim();
            var issn = (r.IssnCode ?? "").Trim();

            Venue? venue = null;

            // Prioriza ISSN; si no, busca por Name
            if (!string.IsNullOrEmpty(issn))
                venue = await _db.Venues.FirstOrDefaultAsync(v => v.IssnCode == issn, ct);
            if (venue == null && !string.IsNullOrEmpty(name))
                venue = await _db.Venues.FirstOrDefaultAsync(v => v.Name == name, ct);

            var created = false;
            if (venue == null)
            {
                venue = new Venue
                {
                    Name = string.IsNullOrEmpty(name) ? "(Sin nombre)" : name,
                    IssnCode = string.IsNullOrEmpty(issn) ? null : issn,
                    IssueNumber = r.IssueNumber,
                    VolumeNumber = r.VolumeNumber,
                    JournalUrl = r.JournalUrl,
                    Type = "Journal"
                };
                _db.Venues.Add(venue);
                created = true;
            }
            else
            {
                // Refresca metadatos básicos si llegan
                venue.IssueNumber = r.IssueNumber ?? venue.IssueNumber;
                venue.VolumeNumber = r.VolumeNumber ?? venue.VolumeNumber;
                venue.JournalUrl = r.JournalUrl ?? venue.JournalUrl;
            }

            await _db.SaveChangesAsync(ct);
            return (venue.VenueId, created);
        }

        private async Task UpsertVenueMetricAsync(
            int venueId,
            short? year,
            decimal? sjr,
            string? quartile,
            CancellationToken ct)
        {
            if (!year.HasValue || (!sjr.HasValue && string.IsNullOrWhiteSpace(quartile)))
                return;

            var metric = await _db.VenueMetrics
                .FirstOrDefaultAsync(m => m.VenueId == venueId && m.Year == year.Value, ct);

            if (metric == null)
            {
                metric = new VenueMetric
                {
                    VenueId = venueId,
                    Year = year.Value,
                    SJR = sjr,
                    Quartile = quartile
                };
                _db.VenueMetrics.Add(metric);
            }
            else
            {
                metric.SJR = sjr ?? metric.SJR;
                metric.Quartile = quartile ?? metric.Quartile;
            }

            await _db.SaveChangesAsync(ct);
        }

        private async Task ReplaceIndexingsAsync(
            int articleId,
            List<int> sourceIds,
            CancellationToken ct)
        {
            var existing = await _db.ArticleIndexings
                .Where(x => x.ArticleId == articleId)
                .ToListAsync(ct);
            if (existing.Count > 0)
                _db.ArticleIndexings.RemoveRange(existing);

            if (sourceIds != null && sourceIds.Count > 0)
            {
                var news = sourceIds
                    .Distinct()
                    .Select(i => new ArticleIndexing
                    {
                        ArticleId = articleId,
                        IndexingSourceId = i
                    });

                await _db.ArticleIndexings.AddRangeAsync(news, ct);
            }

            await _db.SaveChangesAsync(ct);
        }

        private async Task ReplaceParticipantsAsync(
      int articleId,
      List<ArticleParticipantRequest> participants,
      CancellationToken ct)
        {
            // Normaliza & filtra
            var cleaned = (participants ?? new List<ArticleParticipantRequest>())
                .Where(p => !string.IsNullOrWhiteSpace(p?.Nombre))
                .Select(p => new ArticleParticipantRequest
                {
                    Index = p.Index <= 0 ? 1 : p.Index,  // asegura índice >= 1
                    Identificacion = string.IsNullOrWhiteSpace(p.Identificacion) ? null : p.Identificacion.Trim(),
                    Nombre = p.Nombre.Trim(),
                    Participacion = string.IsNullOrWhiteSpace(p.Participacion) ? null : p.Participacion.Trim()
                })
                // Si mandan índices repetidos, mantenemos el primero
                .GroupBy(p => p.Index)
                .Select(g => g.First())
                // Orden consistente
                .OrderBy(p => p.Index)
                .ToList();

            // Borrado de existentes en bloque (si hay)
            var existing = await _db.ArticleParticipants
                .Where(x => x.ArticleId == articleId)
                .ToListAsync(ct);

            if (existing.Count > 0)
                _db.ArticleParticipants.RemoveRange(existing);

            // Alta en bloque
            if (cleaned.Count > 0)
            {
                var news = cleaned.Select(p => new ArticleParticipant
                {
                    ArticleId = articleId,
                    Index = p.Index,
                    Identificacion = p.Identificacion,
                    Nombre = p.Nombre,
                    Participacion = p.Participacion
                });

                await _db.ArticleParticipants.AddRangeAsync(news, ct);
            }
            _logger.LogDebug(
                "Participantes actualizados para ArticleId={ArticleId}. Recibidos={IncomingCount}, guardados={SavedCount}.",
                articleId,
                participants?.Count ?? 0,
                cleaned.Count);

            await _db.SaveChangesAsync(ct);
        }

        private static string NormalizeName(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToUpperInvariant();
        }

        private static bool ParseBool(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            var v = value.Trim().ToLowerInvariant();
            return v == "si" || v == "sí" || v == "true" || v == "1" || v == "yes";
        }

        private static List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            if (line == null)
            {
                result.Add(string.Empty);
                return result;
            }

            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    // Escapar "" dentro de comillas
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++; // saltar la segunda comilla
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ';' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            result.Add(sb.ToString());
            return result;
        }

        private async Task UpsertEvidenceAsync(
    int articleId,
    string? evidenceUrl,
    string? userId,
    CancellationToken ct)
        {
            await Task.CompletedTask;
        }
    }
}
