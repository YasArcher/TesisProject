using tesisproject.shared.DTOs.Reports;

namespace tesisproject.frontend.Features.Management.Components;

public sealed record ReportTrendSummary(double? GrowthPercent, int? StartYear, int? EndYear);

public static class ReportDashboardMetricsBuilder
{
    public static List<(string Name, int Count)> BuildTopBroadFields(IEnumerable<ArticlesByFieldDto>? rows)
        => rows?
            .GroupBy(f => string.IsNullOrWhiteSpace(f.BroadFieldName) ? "Sin \u00e1rea" : f.BroadFieldName!)
            .Select(g => (Name: g.Key, Count: g.Sum(x => x.Count)))
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList() ?? new List<(string Name, int Count)>();

    public static List<(string Name, int Count)> BuildTopResearchLines(IEnumerable<ArticlesByResearchLineDto>? rows)
        => rows?
            .GroupBy(r => string.IsNullOrWhiteSpace(r.ResearchLineName) ? "Sin l\u00ednea" : r.ResearchLineName!)
            .Select(g => (Name: g.Key, Count: g.Sum(x => x.Count)))
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList() ?? new List<(string Name, int Count)>();

    public static ReportTrendSummary BuildTrendSummary(IReadOnlyCollection<ArticlesByYearDto>? rows)
    {
        if (rows == null || rows.Count < 2)
        {
            return new ReportTrendSummary(null, null, null);
        }

        var ordered = rows.OrderBy(x => x.Year).ToList();
        var first = ordered.First();
        var last = ordered.Last();

        if (first.Count <= 0)
        {
            return new ReportTrendSummary(null, first.Year, last.Year);
        }

        var growthPercent = Math.Round(((last.Count - first.Count) / (double)first.Count) * 100.0, 2);

        return new ReportTrendSummary(growthPercent, first.Year, last.Year);
    }
}
