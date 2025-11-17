using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;
using tesisproject.backend.Mapping;               // ToListItemDto / ToDetailDto
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Services.Implementations
{
    public class ArticlesService : IArticlesService
    {
        private readonly AppDbContext _db;

        public ArticlesService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
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
        // DETAIL
        // =========================================================
        public async Task<ArticleDetailDto?> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            var entity = await _db.Articles
                .Include(a => a.Venue)!.ThenInclude(v => v!.VenueMetrics)
                .Include(a => a.Indexings)!.ThenInclude(ix => ix.IndexingSource)
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
                ProjectId = request.ProjectId,

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
            article.ProjectId = request.ProjectId;

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

            // 🔎 Log mínimo para verificar en consola
            // (si no quieres logs, puedes quitarlo)
            Console.WriteLine($"[Participants] ArticleId={articleId} Incoming={participants?.Count ?? 0} Saved={cleaned.Count}");

            await _db.SaveChangesAsync(ct);
        }


        private async Task UpsertEvidenceAsync(
    int articleId,
    string? evidenceUrl,
    string? userId,
    CancellationToken ct)
        {
            // Tu entidad ArticleFile no tiene propiedad 'Url'.
            // Dejo este método inofensivo para no romper el flujo.
            // Cuando me confirmes el/los campos (p.ej. Path, FilePath, StorageUrl),
            // activo la lógica de guardado/actualización aquí.

            await Task.CompletedTask;
        }
    }
}
