using tesisproject.frontend.Models;
using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.Services.Implementations.Mocks;

public sealed class PredictMockService : IPredictService
{
    public Task<IReadOnlyList<PredictionPoint>> GetSeriesAsync(string metric, int horizonMonths, Filters filters, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = today.AddMonths(-24);
        var list = new List<PredictionPoint>();

        // history (24 months)
        for (int i = 0; i < 24; i++)
        {
            var t = start.AddMonths(i);
            var val = 30 + 5 * Math.Sin(i / 4.0);
            list.Add(new PredictionPoint(t, Math.Round(val, 2)));
        }

        // simple linear forecast
        var last = list.Last().Value;
        for (int i = 1; i <= horizonMonths; i++)
        {
            var t = today.AddMonths(i);
            var val = last + i * 1.2;
            list.Add(new PredictionPoint(t, Math.Round(val, 2), Lower: val - 2, Upper: val + 2));
        }
        return Task.FromResult((IReadOnlyList<PredictionPoint>)list);
    }
}