using tesisproject.shared.DTOs.Reports;

namespace tesisproject.frontend.Features.Management.Components;

public static class ReportDashboardFilterBuilder
{
    public static ArticlesDashboardFilterDto Build(
        DateTime? createdFrom,
        DateTime? createdTo,
        DateTime? publicationFrom,
        DateTime? publicationTo,
        string? isOpenAccess,
        string? quartile,
        int academicTermId,
        int projectId,
        int researchLineId,
        int broadFieldId,
        int specificFieldId,
        int detailedFieldId,
        int publicationStatusId,
        int venueId,
        int indexingSourceId)
    {
        var filter = new ArticlesDashboardFilterDto
        {
            CreatedFrom = createdFrom,
            CreatedTo = createdTo,
            PublicationFrom = publicationFrom,
            PublicationTo = publicationTo
        };

        if (!string.IsNullOrEmpty(isOpenAccess) &&
            bool.TryParse(isOpenAccess, out var oaValue))
        {
            filter.IsOpenAccess = oaValue;
        }

        if (!string.IsNullOrEmpty(quartile))
        {
            filter.Quartiles = new List<string> { quartile };
        }

        if (academicTermId > 0)
            filter.AcademicTermKeys = new List<int> { academicTermId };

        if (projectId > 0)
            filter.ProjectKeys = new List<int> { projectId };

        if (researchLineId > 0)
            filter.ResearchLineKeys = new List<int> { researchLineId };

        if (detailedFieldId > 0)
            filter.FieldKeys = new List<int> { detailedFieldId };
        else if (specificFieldId > 0)
            filter.FieldKeys = new List<int> { specificFieldId };
        else if (broadFieldId > 0)
            filter.FieldKeys = new List<int> { broadFieldId };

        if (publicationStatusId > 0)
            filter.PublicationStatusKeys = new List<int> { publicationStatusId };

        if (venueId > 0)
            filter.VenueKeys = new List<int> { venueId };

        if (indexingSourceId > 0)
            filter.IndexingSourceKeys = new List<int> { indexingSourceId };

        return filter;
    }
}
