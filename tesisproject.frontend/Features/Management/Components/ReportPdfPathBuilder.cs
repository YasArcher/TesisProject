namespace tesisproject.frontend.Features.Management.Components;

public static class ReportPdfPathBuilder
{
    public static string BuildArticlesStatisticsPath(
        DateTime? createdFrom,
        DateTime? createdTo,
        bool includeKpis,
        bool includeByYear,
        bool includeByField,
        bool includeByResearchLine,
        bool includeQuartiles)
    {
        var parts = new List<string>();

        if (createdFrom.HasValue)
            parts.Add($"minYear={createdFrom.Value.Year}");

        if (createdTo.HasValue)
            parts.Add($"maxYear={createdTo.Value.Year}");

        parts.Add($"includeKpis={includeKpis.ToString().ToLower()}");
        parts.Add($"includeByYear={includeByYear.ToString().ToLower()}");
        parts.Add($"includeByField={includeByField.ToString().ToLower()}");
        parts.Add($"includeByResearchLine={includeByResearchLine.ToString().ToLower()}");
        parts.Add($"includeQuartiles={includeQuartiles.ToString().ToLower()}");

        var query = parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;

        return $"api/reports/articles/statistics/pdf{query}";
    }
}
