using tesisproject.frontend.Models;
using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.Features.Management.State;

public sealed class DashboardStore
{
    private readonly IInsightsService _svc;
    public Filters Filters { get; private set; } = new();
    public DashboardKpis? Kpis { get; private set; }
    public List<CountByYear> ByYear { get; } = new();
    public List<TopItem> TopAuthors { get; } = new();
    public List<TopItem> TopVenues { get; } = new();
    public int Open { get; private set; }
    public int Closed { get; private set; }
    public bool IsLoading { get; private set; }

    public DashboardStore(IInsightsService svc) => _svc = svc;

    public async Task LoadAsync(Filters filters, CancellationToken ct = default)
    {
        IsLoading = true;
        Filters = filters;

        Kpis = await _svc.GetKpisAsync(filters, ct);
        ByYear.Clear();
        ByYear.AddRange(await _svc.GetCountByYearAsync(filters, ct));
        TopAuthors.Clear();
        TopAuthors.AddRange(await _svc.GetTopAuthorsAsync(filters, 10, ct));
        TopVenues.Clear();
        TopVenues.AddRange(await _svc.GetTopVenuesAsync(filters, 10, ct));
        (Open, Closed) = await _svc.GetOpenAccessStatsAsync(filters, ct);

        IsLoading = false;
    }
}