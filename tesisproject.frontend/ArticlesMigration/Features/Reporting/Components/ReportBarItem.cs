namespace tesisproject.frontend.Features.Reporting.Components;

public sealed record ReportBarItem(
    string Label,
    int Value,
    string? Title = null,
    decimal? Percentage = null);
