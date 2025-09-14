using tesisproject.frontend.Models;
using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.Features.Management.State;

public sealed class AIStore
{
    private readonly IRecService _rec;
    private readonly IPredictService _pred;

    public int ActiveTab { get; private set; } // 0 = Recs, 1 = Preds

    // Recs
    public string Seed { get; private set; } = string.Empty;
    public List<RecommendationItem> Recs { get; } = new();
    public bool IsLoadingRecs { get; private set; }

    // Preds
    public string Metric { get; private set; } = "publications";
    public int Horizon { get; private set; } = 12;
    public List<PredictionPoint> Series { get; } = new();
    public bool IsLoadingPreds { get; private set; }

    public AIStore(IRecService rec, IPredictService pred)
    {
        _rec = rec;
        _pred = pred;
    }

    public void SetTab(int tab) => ActiveTab = tab;

    public async Task LoadRecsAsync(string seed, Filters filters, CancellationToken ct = default)
    {
        IsLoadingRecs = true;
        Seed = seed;
        Recs.Clear();
        Recs.AddRange(await _rec.GetRecommendationsAsync(seed, filters, ct));
        IsLoadingRecs = false;
    }

    public async Task LoadPredsAsync(string metric, int horizon, Filters filters, CancellationToken ct = default)
    {
        IsLoadingPreds = true;
        Metric = metric;
        Horizon = horizon;
        Series.Clear();
        Series.AddRange(await _pred.GetSeriesAsync(metric, horizon, filters, ct));
        IsLoadingPreds = false;
    }
}