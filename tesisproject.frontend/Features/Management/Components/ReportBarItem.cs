namespace tesisproject.frontend.Features.Management.Components;

public sealed record ReportBarItem(
    string Label,
    int Value,
    string? Title = null,
    decimal? Percentage = null);
