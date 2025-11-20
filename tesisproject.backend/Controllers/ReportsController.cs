using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.DataWarehouse;
using tesisproject.shared.DTOs.Reports; 

namespace tesisproject.backend.BI.Reports
{
    [ApiController]
    [Route("api/reports")]
    //[Authorize] 
    [AllowAnonymous]
    public class ReportsController : ControllerBase
    {
        private readonly DwDbContext _dw;

        public ReportsController(DwDbContext dw)
        {
            _dw = dw;
        }

        [HttpGet("articles/by-year")]
        public async Task<ActionResult<IEnumerable<ArticlesByYearDto>>> GetArticlesByYear()
        {
            var query = await _dw.FactArticlePublications
                .Include(f => f.CreatedDate)
                .GroupBy(f => f.CreatedDate.Year)
                .Select(g => new ArticlesByYearDto
                {
                    Year = g.Key,
                    Count = g.Sum(x => x.ArticleCount)
                })
                .OrderBy(x => x.Year)
                .ToListAsync();

            return Ok(query);
        }

        [HttpGet("articles/by-field")]
        public async Task<ActionResult<IEnumerable<ArticlesByFieldDto>>> GetArticlesByField()
        {
            var query = await _dw.FactArticlePublications
                .Include(f => f.Field)
                .Where(f => f.FieldKey != null)
                .GroupBy(f => new
                {
                    f.Field!.BroadFieldName,
                    f.Field!.SpecificFieldName,
                    f.Field!.DetailedFieldName
                })
                .Select(g => new ArticlesByFieldDto
                {
                    BroadFieldName = g.Key.BroadFieldName,
                    SpecificFieldName = g.Key.SpecificFieldName,
                    DetailedFieldName = g.Key.DetailedFieldName,
                    Count = g.Sum(x => x.ArticleCount)
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            return Ok(query);
        }

        [HttpGet("articles/by-research-line")]
        public async Task<ActionResult<IEnumerable<ArticlesByResearchLineDto>>> GetArticlesByResearchLine()
        {
            var query = await _dw.FactArticlePublications
                .Include(f => f.ResearchLine)
                .Where(f => f.ResearchLineKey != null)
                .GroupBy(f => f.ResearchLine!.Name)
                .Select(g => new ArticlesByResearchLineDto
                {
                    ResearchLineName = g.Key,
                    Count = g.Sum(x => x.ArticleCount)
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            return Ok(query);
        }

        [HttpGet("articles/kpis/summary")]
        public async Task<ActionResult<ArticlesKpiSummaryDto>> GetArticlesKpiSummary()
        {
            var nowYear = DateTime.UtcNow.Year;
            var facts = _dw.FactArticlePublications.AsQueryable();

            var totalArticles = await facts.CountAsync();

            var publishedThisYear = await facts
                .Include(f => f.PublicationDate)
                .Where(f => f.PublicationDate != null && f.PublicationDate!.Year == nowYear)
                .CountAsync();

            var q1q2Count = await facts
                .Where(f => f.Quartile == "Q1" || f.Quartile == "Q2")
                .CountAsync();

            var openAccessCount = await facts
                .Where(f => f.IsOpenAccess)
                .CountAsync();

            var scopusIndexed = await _dw.FactArticleIndexings
                .Include(fi => fi.IndexingSource)
                .Where(fi => fi.IndexingSource.Name == "Scopus")
                .Select(fi => fi.ArticleKey)
                .Distinct()
                .CountAsync();

            var dto = new ArticlesKpiSummaryDto
            {
                TotalArticles = totalArticles,
                PublishedThisYear = publishedThisYear,
                Q1Q2Count = q1q2Count,
                OpenAccessCount = openAccessCount,
                IndexedInScopusCount = scopusIndexed
            };

            return Ok(dto);
        }
    }
}
