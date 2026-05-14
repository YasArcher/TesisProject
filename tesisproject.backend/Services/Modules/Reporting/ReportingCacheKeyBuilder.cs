using tesisproject.shared.DTOs.Reports;

namespace tesisproject.backend.Services.Modules.Reporting;

internal static class ReportingCacheKeyBuilder
{
    public static string BuildDashboardKey(InstitutionalReportingFilterDto? filter, int cacheVersion)
    {
        if (filter is null)
        {
            return $"reporting:dashboard:{cacheVersion}:empty";
        }

        return string.Join('|',
            "reporting:dashboard",
            cacheVersion,
            filter.CreatedFrom?.ToString("yyyyMMdd"),
            filter.CreatedTo?.ToString("yyyyMMdd"),
            filter.PublishedFrom?.ToString("yyyyMMdd"),
            filter.PublishedTo?.ToString("yyyyMMdd"),
            filter.ArticleTitle,
            filter.ArticleDoi,
            filter.ProjectName,
            filter.AcademicTerm,
            filter.PublicationStatus,
            filter.ResearchLine,
            filter.Faculty,
            filter.IndexingSource,
            filter.BroadField,
            filter.SpecificField,
            filter.DetailedField,
            filter.VenueName,
            filter.VenueType,
            filter.ArticleYear,
            filter.ArticleMonth,
            filter.Quartile,
            filter.IsOpenAccess,
            filter.IsProjectResult,
            filter.HasInterculturalComponent,
            filter.PeriodDateType,
            filter.AuthorName,
            filter.AuthorAffiliation,
            filter.ParticipantType,
            filter.HasOrcid,
            filter.OnlyPrimaryAuthors,
            filter.CoauthorName);
    }

    public static string BuildAuthorKey(InstitutionalReportingFilterDto? filter, int cacheVersion)
        => $"reporting:authors:{BuildDashboardKey(filter, cacheVersion)}";
}
