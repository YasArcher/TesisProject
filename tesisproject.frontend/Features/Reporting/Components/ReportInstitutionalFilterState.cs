using tesisproject.shared.DTOs.Reports;

namespace tesisproject.frontend.Features.Reporting.Components;

public static class ReportInstitutionalFilterState
{
    public static string GetPeriodLabel(InstitutionalReportingFilterDto filter)
        => string.Equals(filter.PeriodDateType, "created", StringComparison.OrdinalIgnoreCase)
            ? "Fecha de registro"
            : "Fecha de publicación";

    public static IReadOnlyList<(string Label, string Value)> BuildActiveChips(InstitutionalReportingFilterDto filter)
    {
        var chips = new List<(string Label, string Value)>();

        AddDateChip(chips, "Creación desde", filter.CreatedFrom);
        AddDateChip(chips, "Creación hasta", filter.CreatedTo);
        AddDateChip(chips, "Publicación desde", filter.PublishedFrom);
        AddDateChip(chips, "Publicación hasta", filter.PublishedTo);
        AddChip(chips, "Título", filter.ArticleTitle);
        AddChip(chips, "DOI", filter.ArticleDoi);
        AddChip(chips, "Proyecto", filter.ProjectName);
        AddChip(chips, "Periodo", filter.AcademicTerm);
        AddChip(chips, "Estado", filter.PublicationStatus);
        AddChip(chips, "Línea", filter.ResearchLine);
        AddChip(chips, "Facultad", filter.Faculty);
        AddChip(chips, "Base de datos", filter.IndexingSource);
        AddChip(chips, "Campo amplio", filter.BroadField);
        AddChip(chips, "Campo específico", filter.SpecificField);
        AddChip(chips, "Campo detallado", filter.DetailedField);
        AddChip(chips, "Revista", filter.VenueName);
        AddChip(chips, "Tipo de publicación", filter.VenueType);
        AddChip(chips, "Cuartil", filter.Quartile);
        AddChip(chips, "Autor", filter.AuthorName);
        AddChip(chips, "Coautor", filter.CoauthorName);
        AddChip(chips, "Filiación autor", filter.AuthorAffiliation);
        AddChip(chips, "Tipo participante", filter.ParticipantType);

        if (filter.HasOrcid.HasValue)
        {
            chips.Add(("ORCID", filter.HasOrcid.Value ? "Con ORCID" : "Sin ORCID"));
        }

        if (!string.IsNullOrWhiteSpace(filter.PeriodDateType))
        {
            AddChip(chips, "Periodo temporal", GetPeriodLabel(filter));
        }

        if (filter.ArticleYear.HasValue)
        {
            chips.Add(("Año", filter.ArticleYear.Value.ToString()));
        }

        AddChip(chips, "Mes", filter.ArticleMonth);

        if (filter.IsOpenAccess.HasValue)
        {
            chips.Add(("Acceso", filter.IsOpenAccess.Value ? "Open Access" : "No Open Access"));
        }

        if (filter.IsProjectResult.HasValue)
        {
            chips.Add(("Proyecto", filter.IsProjectResult.Value ? "Sí" : "No"));
        }

        if (filter.HasInterculturalComponent.HasValue)
        {
            chips.Add(("Interculturalidad", filter.HasInterculturalComponent.Value ? "Sí" : "No"));
        }

        if (filter.OnlyPrimaryAuthors == true)
        {
            chips.Add(("Autoría", "Solo autor principal"));
        }

        return chips;
    }

    public static InstitutionalReportingFilterDto Clone(InstitutionalReportingFilterDto source)
        => new()
        {
            CreatedFrom = source.CreatedFrom,
            CreatedTo = source.CreatedTo,
            PublishedFrom = source.PublishedFrom,
            PublishedTo = source.PublishedTo,
            ArticleTitle = source.ArticleTitle,
            ArticleDoi = source.ArticleDoi,
            ProjectName = source.ProjectName,
            AcademicTerm = source.AcademicTerm,
            PublicationStatus = source.PublicationStatus,
            ResearchLine = source.ResearchLine,
            Faculty = source.Faculty,
            IndexingSource = source.IndexingSource,
            BroadField = source.BroadField,
            SpecificField = source.SpecificField,
            DetailedField = source.DetailedField,
            VenueName = source.VenueName,
            VenueType = source.VenueType,
            ArticleYear = source.ArticleYear,
            ArticleMonth = source.ArticleMonth,
            Quartile = source.Quartile,
            IsOpenAccess = source.IsOpenAccess,
            IsProjectResult = source.IsProjectResult,
            HasInterculturalComponent = source.HasInterculturalComponent,
            PeriodDateType = source.PeriodDateType,
            AuthorName = source.AuthorName,
            AuthorAffiliation = source.AuthorAffiliation,
            ParticipantType = source.ParticipantType,
            HasOrcid = source.HasOrcid,
            OnlyPrimaryAuthors = source.OnlyPrimaryAuthors,
            CoauthorName = source.CoauthorName,
            IncludePdfKpis = source.IncludePdfKpis,
            IncludePdfFilters = source.IncludePdfFilters,
            IncludePdfCharts = source.IncludePdfCharts,
            IncludePdfPeriod = source.IncludePdfPeriod,
            IncludePdfFields = source.IncludePdfFields,
            IncludePdfVenues = source.IncludePdfVenues,
            IncludePdfAuthors = source.IncludePdfAuthors,
            IncludePdfPediIiit = source.IncludePdfPediIiit,
            IncludePdfTddTotal = source.IncludePdfTddTotal,
            IncludePdfParticipation = source.IncludePdfParticipation,
            IncludePdfArticles = source.IncludePdfArticles
        };

    private static void AddChip(List<(string Label, string Value)> chips, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            chips.Add((label, value.Trim()));
        }
    }

    private static void AddDateChip(List<(string Label, string Value)> chips, string label, DateTime? value)
    {
        if (value.HasValue)
        {
            chips.Add((label, value.Value.ToString("dd/MM/yyyy")));
        }
    }
}
