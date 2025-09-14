using tesisproject.frontend.Models;
using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.Services.Implementations.Mocks;

public sealed class InsightsMockService : IInsightsService
{
    private static readonly List<ArticleDto> _seed = SeedArticles();

    public Task<DashboardKpis> GetKpisAsync(Filters filters, CancellationToken ct = default)
    {
        var data = Apply(filters);
        var total = data.Count;
        var open = data.Count(a => a.OpenAccess);
        var citations = data.Sum(a => a.CitationCount ?? 0);
        var medianYear = total == 0 ? 0 : data.OrderBy(a => a.Year).ElementAt((total - 1) / 2).Year;
        var kpis = new DashboardKpis(total, total == 0 ? 0 : (double)open / total * 100.0, medianYear, citations);
        return Task.FromResult(kpis);
    }

    public Task<IReadOnlyList<CountByYear>> GetCountByYearAsync(Filters filters, CancellationToken ct = default)
    {
        var data = Apply(filters)
            .GroupBy(a => a.Year)
            .OrderBy(g => g.Key)
            .Select(g => new CountByYear(g.Key, g.Count()))
            .ToList();
        return Task.FromResult((IReadOnlyList<CountByYear>)data);
    }

    public Task<IReadOnlyList<TopItem>> GetTopAuthorsAsync(Filters filters, int topN = 10, CancellationToken ct = default)
    {
        var data = Apply(filters)
            .SelectMany(a => a.Authors)
            .GroupBy(a => a)
            .OrderByDescending(g => g.Count())
            .Take(topN)
            .Select(g => new TopItem(g.Key, g.Count()))
            .ToList();
        return Task.FromResult((IReadOnlyList<TopItem>)data);
    }

    public Task<IReadOnlyList<TopItem>> GetTopVenuesAsync(Filters filters, int topN = 10, CancellationToken ct = default)
    {
        var data = Apply(filters)
            .GroupBy(a => a.Venue)
            .OrderByDescending(g => g.Count())
            .Take(topN)
            .Select(g => new TopItem(g.Key, g.Count()))
            .ToList();
        return Task.FromResult((IReadOnlyList<TopItem>)data);
    }

    public Task<(int Open, int Closed)> GetOpenAccessStatsAsync(Filters filters, CancellationToken ct = default)
    {
        var data = Apply(filters);
        var open = data.Count(a => a.OpenAccess);
        return Task.FromResult((open, data.Count - open));
    }

    private static List<ArticleDto> Apply(Filters f)
    {
        IEnumerable<ArticleDto> q = _seed;
        if (!string.IsNullOrWhiteSpace(f.Query))
        {
            var s = f.Query.Trim().ToLowerInvariant();
            q = q.Where(a => a.Title.ToLower().Contains(s)
                          || a.Authors.Any(x => x.ToLower().Contains(s))
                          || a.Keywords.Any(k => k.ToLower().Contains(s)));
        }
        if (f.From.HasValue) q = q.Where(a => a.Year >= f.From);
        if (f.To.HasValue) q = q.Where(a => a.Year <= f.To);
        if (!string.IsNullOrWhiteSpace(f.Venue)) q = q.Where(a => a.Venue == f.Venue);
        if (!string.IsNullOrWhiteSpace(f.Author)) q = q.Where(a => a.Authors.Contains(f.Author));
        if (f.OpenAccess.HasValue) q = q.Where(a => a.OpenAccess == f.OpenAccess);
        if (f.Tags is { Count: > 0 }) q = q.Where(a => a.Keywords.Intersect(f.Tags!).Any());
        return q.ToList();
    }

    private static List<ArticleDto> SeedArticles()
    {
        var rng = new Random(42);
        var venues = new[] { "Heliyon", "IM", "JMSACL", "Procedia CS", "IEEE Access" };
        var keywords = new[] { "deep learning", "recommender", "forecast", "NLP", "dashboard", "ETL" };
        var list = new List<ArticleDto>();
        for (int i = 0; i < 120; i++)
        {
            list.Add(new ArticleDto
            {
                Title = $"Sample Article {i:000}",
                Authors = new() { $"Author {i % 7}", $"Author {(i + 3) % 11}" },
                Venue = venues[i % venues.Length],
                Year = 2010 + (i % 15),
                Keywords = new() { keywords[i % keywords.Length], keywords[(i + 2) % keywords.Length] },
                Doi = $"10.1000/sample.{i}",
                Url = "https://example.com",
                OpenAccess = i % 3 != 0,
                CitationCount = rng.Next(0, 250)
            });
        }
        return list;
    }
}