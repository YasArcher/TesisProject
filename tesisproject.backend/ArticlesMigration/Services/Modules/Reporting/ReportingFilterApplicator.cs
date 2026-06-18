using System.Linq.Expressions;
using tesisproject.backend.Reporting.Models;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.backend.Services.Modules.Reporting;

internal static class ReportingFilterApplicator
{
    public static IQueryable<ReportingArticleDetailRow> ApplyArticleDetailFilters(
        IQueryable<ReportingArticleDetailRow> query,
        InstitutionalReportingFilterDto? filter,
        IQueryable<ReportingVenueMetricRow> venueMetrics)
    {
        if (filter is null)
        {
            return query;
        }

        if (filter.CreatedFrom.HasValue)
        {
            query = query.Where(x => x.CreatedDate >= filter.CreatedFrom.Value.Date);
        }

        if (filter.CreatedTo.HasValue)
        {
            query = query.Where(x => x.CreatedDate < filter.CreatedTo.Value.Date.AddDays(1));
        }

        if (filter.PublishedFrom.HasValue)
        {
            query = query.Where(x => x.PublishedDate >= filter.PublishedFrom.Value.Date);
        }

        if (filter.PublishedTo.HasValue)
        {
            query = query.Where(x => x.PublishedDate < filter.PublishedTo.Value.Date.AddDays(1));
        }

        query = ApplyContainsFilter(query, filter.ArticleTitle, x => x.Title);
        query = ApplyContainsFilter(query, filter.ArticleDoi, x => x.Doi);
        query = ApplyStringFilter(query, filter.AcademicTerm, x => x.AcademicTerm);
        query = ApplyStringFilter(query, filter.PublicationStatus, x => x.PublicationStatus);
        query = ApplyStringFilter(query, filter.ResearchLine, x => x.ResearchLine);
        query = ApplyStringFilter(query, filter.Faculty, x => x.FacultyName);
        query = ApplyStringFilter(query, filter.BroadField, x => x.BroadFieldName);
        query = ApplyStringFilter(query, filter.SpecificField, x => x.SpecificFieldName);
        query = ApplyStringFilter(query, filter.DetailedField, x => x.DetailedFieldName);
        query = ApplyStringFilter(query, filter.VenueName, x => x.VenueName);
        query = ApplyStringFilter(query, filter.VenueType, x => x.VenueType);

        if (filter.ArticleYear.HasValue)
        {
            query = query.Where(x => x.ArticleYear == filter.ArticleYear.Value);
        }

        query = ApplyMonthFilter(query, filter);

        query = ApplyQuartileFilter(query, filter, venueMetrics);

        if (filter.IsOpenAccess.HasValue)
        {
            query = query.Where(x => x.IsOpenAccess == filter.IsOpenAccess.Value);
        }

        if (filter.IsProjectResult.HasValue)
        {
            query = query.Where(x => x.IsProjectResult == filter.IsProjectResult.Value);
        }

        if (filter.HasInterculturalComponent.HasValue)
        {
            query = query.Where(x => x.HasInterculturalComponent == filter.HasInterculturalComponent.Value);
        }

        return query;
    }

    public static bool HasScopedInstitutionalFilters(InstitutionalReportingFilterDto? filter)
    {
        if (filter is null)
        {
            return false;
        }

        return filter.CreatedFrom.HasValue
            || filter.CreatedTo.HasValue
            || filter.PublishedFrom.HasValue
            || filter.PublishedTo.HasValue
            || !string.IsNullOrWhiteSpace(filter.ArticleTitle)
            || !string.IsNullOrWhiteSpace(filter.ArticleDoi)
            || !string.IsNullOrWhiteSpace(filter.AcademicTerm)
            || !string.IsNullOrWhiteSpace(filter.PublicationStatus)
            || !string.IsNullOrWhiteSpace(filter.ResearchLine)
            || !string.IsNullOrWhiteSpace(filter.Faculty)
            || !string.IsNullOrWhiteSpace(filter.IndexingSource)
            || !string.IsNullOrWhiteSpace(filter.BroadField)
            || !string.IsNullOrWhiteSpace(filter.SpecificField)
            || !string.IsNullOrWhiteSpace(filter.DetailedField)
            || !string.IsNullOrWhiteSpace(filter.VenueName)
            || !string.IsNullOrWhiteSpace(filter.VenueType)
            || filter.ArticleYear.HasValue
            || !string.IsNullOrWhiteSpace(filter.ArticleMonth)
            || !string.IsNullOrWhiteSpace(filter.Quartile)
            || filter.IsOpenAccess.HasValue
            || filter.IsProjectResult.HasValue
            || filter.HasInterculturalComponent.HasValue
            || !string.IsNullOrWhiteSpace(filter.ProjectName);
    }

    public static bool HasAuthorScopedFilters(InstitutionalReportingFilterDto? filter)
    {
        if (filter is null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(filter.AuthorName)
            || !string.IsNullOrWhiteSpace(filter.CoauthorName)
            || !string.IsNullOrWhiteSpace(filter.AuthorAffiliation)
            || !string.IsNullOrWhiteSpace(filter.ParticipantType)
            || filter.HasOrcid.HasValue
            || filter.OnlyPrimaryAuthors == true;
    }

    private static IQueryable<ReportingArticleDetailRow> ApplyQuartileFilter(
        IQueryable<ReportingArticleDetailRow> query,
        InstitutionalReportingFilterDto filter,
        IQueryable<ReportingVenueMetricRow> venueMetrics)
    {
        if (string.IsNullOrWhiteSpace(filter.Quartile))
        {
            return query;
        }

        var quartile = filter.Quartile.Trim();
        if (string.Equals(quartile, "Sin cuartil", StringComparison.OrdinalIgnoreCase))
        {
            return query.Where(x =>
                string.IsNullOrEmpty(
                    venueMetrics
                        .Where(metric =>
                            metric.VenueName == x.VenueName
                            && (!x.ArticleYear.HasValue || metric.YearNumber <= x.ArticleYear.Value))
                        .OrderByDescending(metric => metric.YearNumber)
                        .Select(metric => metric.Quartile)
                        .FirstOrDefault()));
        }

        return query.Where(x =>
            venueMetrics
                .Where(metric =>
                    metric.VenueName == x.VenueName
                    && (!x.ArticleYear.HasValue || metric.YearNumber <= x.ArticleYear.Value))
                .OrderByDescending(metric => metric.YearNumber)
                .Select(metric => metric.Quartile)
                .FirstOrDefault() == quartile);
    }

    private static IQueryable<ReportingArticleDetailRow> ApplyMonthFilter(
        IQueryable<ReportingArticleDetailRow> query,
        InstitutionalReportingFilterDto filter)
    {
        if (string.IsNullOrWhiteSpace(filter.ArticleMonth)
            || !DateTime.TryParse($"{filter.ArticleMonth.Trim()}-01", out var monthStart))
        {
            return query;
        }

        var monthEnd = monthStart.AddMonths(1);
        return string.Equals(filter.PeriodDateType, "created", StringComparison.OrdinalIgnoreCase)
            ? query.Where(x => x.CreatedDate >= monthStart && x.CreatedDate < monthEnd)
            : query.Where(x => (x.PublishedDate ?? x.CreatedDate) >= monthStart && (x.PublishedDate ?? x.CreatedDate) < monthEnd);
    }

    private static IQueryable<ReportingArticleDetailRow> ApplyStringFilter(
        IQueryable<ReportingArticleDetailRow> query,
        string? value,
        Expression<Func<ReportingArticleDetailRow, string?>> selector)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return query;
        }

        return query.Where(ExpressionEqual(selector, value.Trim()));
    }

    private static IQueryable<ReportingArticleDetailRow> ApplyContainsFilter(
        IQueryable<ReportingArticleDetailRow> query,
        string? value,
        Expression<Func<ReportingArticleDetailRow, string?>> selector)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return query;
        }

        return query.Where(ExpressionContains(selector, value.Trim()));
    }

    private static Expression<Func<ReportingArticleDetailRow, bool>> ExpressionEqual(
        Expression<Func<ReportingArticleDetailRow, string?>> selector,
        string value)
    {
        var body = Expression.Equal(
            selector.Body,
            Expression.Constant(value));

        return Expression.Lambda<Func<ReportingArticleDetailRow, bool>>(body, selector.Parameters);
    }

    private static Expression<Func<ReportingArticleDetailRow, bool>> ExpressionContains(
        Expression<Func<ReportingArticleDetailRow, string?>> selector,
        string value)
    {
        var notNull = Expression.NotEqual(selector.Body, Expression.Constant(null, typeof(string)));
        var contains = Expression.Call(
            selector.Body,
            nameof(string.Contains),
            Type.EmptyTypes,
            Expression.Constant(value));
        var body = Expression.AndAlso(notNull, contains);

        return Expression.Lambda<Func<ReportingArticleDetailRow, bool>>(body, selector.Parameters);
    }
}
