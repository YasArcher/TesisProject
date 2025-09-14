using tesisproject.frontend.Models;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IRecService
    {
    Task<IReadOnlyList<RecommendationItem>> GetRecommendationsAsync(
    string seed,
    Filters filters,
    CancellationToken ct = default);

    }
}
