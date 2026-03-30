using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;
using tesisproject.backend.Mapping;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.DTOs;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ArticlesRepository : IArticlesRepository
    {
        private readonly AppDbContext _db;

        public ArticlesRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<ArticleListItemDto>> GetListAsync(ArticleListQuery query, CancellationToken ct)
        {
            var q = _db.Articles
                .AsNoTracking()
                .Include(a => a.Venue)
                .Include(a => a.PublicationStatus)
                .Include(a => a.ResearchLine)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
                q = q.Where(a =>
                    (a.Title != null && a.Title.Contains(s)) ||
                    (a.Doi != null && a.Doi.Contains(s)));
            }

            if (query.Year.HasValue)
                q = q.Where(a => a.Year == query.Year);

            if (query.VenueId.HasValue)
                q = q.Where(a => a.VenueId == query.VenueId);

            if (query.PublicationStatusId.HasValue)
                q = q.Where(a => a.PublicationStatusId == query.PublicationStatusId);

            if (query.ResearchLineId.HasValue)
                q = q.Where(a => a.ResearchLineId == query.ResearchLineId);

            if (query.BroadFieldId.HasValue)
                q = q.Where(a => a.BroadFieldId == query.BroadFieldId);

            if (query.SpecificFieldId.HasValue)
                q = q.Where(a => a.SpecificFieldId == query.SpecificFieldId);

            if (query.DetailedFieldId.HasValue)
                q = q.Where(a => a.DetailedFieldId == query.DetailedFieldId);

            // Orden por defecto: CreatedAt desc
            if (query.SortBy?.ToLower() == "year")
                q = query.SortDesc ? q.OrderByDescending(a => a.Year) : q.OrderBy(a => a.Year);
            else
                q = query.SortDesc ? q.OrderByDescending(a => a.CreatedAt) : q.OrderBy(a => a.CreatedAt);

            var total = await q.CountAsync(ct);

            var items = await q
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(a => a.ToListItemDto())
                .ToListAsync(ct);

            return new PagedResult<ArticleListItemDto>(items, total, query.Page, query.PageSize);
        }

        public async Task<Article?> GetByIdWithDetailsAsync(int id, CancellationToken ct)
        {
            return await _db.Articles
                .Include(a => a.Venue)
                .Include(a => a.AcademicTerm)
                .Include(a => a.PublicationStatus)
                .Include(a => a.ResearchLine)
                .Include(a => a.BroadField)
                .Include(a => a.SpecificField)
                .Include(a => a.DetailedField)
                .Include(a => a.Participants)
                .Include(a => a.Files)
                .Include(a => a.Indexings).ThenInclude(i => i.IndexingSource)
                .FirstOrDefaultAsync(a => a.Id == id, ct);
        }

        public async Task AddAsync(Article article, CancellationToken ct)
        {
            await _db.Articles.AddAsync(article, ct);
        }

        public void Remove(Article article)
        {
            _db.Articles.Remove(article);
        }
    }
}
