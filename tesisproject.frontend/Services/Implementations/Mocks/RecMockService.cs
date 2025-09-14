using tesisproject.frontend.Models;
using tesisproject.frontend.Services.Interfaces;

namespace tesisproject.frontend.Services.Implementations.Mocks;

public sealed class RecMockService : IRecService
{
    public Task<IReadOnlyList<RecommendationItem>> GetRecommendationsAsync(string seed, Filters filters, CancellationToken ct = default)
    {
        var items = Enumerable.Range(1, 8)
            .Select(i => new RecommendationItem(
                ArticleId: $"A{i:000}",
                Score: Math.Round(0.9 - i * 0.07, 2),
                Why: new List<string> { "shared keywords", "same venue", i % 2 == 0 ? "co-author" : "recent" }))
            .ToList();
        return Task.FromResult((IReadOnlyList<RecommendationItem>)items);
    }
}