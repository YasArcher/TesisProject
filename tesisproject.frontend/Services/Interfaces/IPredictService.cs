using tesisproject.frontend.Models;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IPredictService
    {
       Task<IReadOnlyList<PredictionPoint>> GetSeriesAsync(
       string metric,
       int horizonMonths,
       Filters filters,
       CancellationToken ct = default);
    }
}
