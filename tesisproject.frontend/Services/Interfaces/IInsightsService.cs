using tesisproject.frontend.Models;
namespace tesisproject.frontend.Services.Interfaces
{
    public interface IInsightsService
    {
        Task<DashboardKpis> GetKpisAsync(Filters filters, CancellationToken ct = default);
        Task<IReadOnlyList<CountByYear>> GetCountByYearAsync(Filters filters, CancellationToken ct = default);
        Task<IReadOnlyList<TopItem>> GetTopAuthorsAsync(Filters filters, int topN = 10, CancellationToken ct = default);
        Task<IReadOnlyList<TopItem>> GetTopVenuesAsync(Filters filters, int topN = 10, CancellationToken ct = default);
        Task<(int Open, int Closed)> GetOpenAccessStatsAsync(Filters filters, CancellationToken ct = default);
    }
}
