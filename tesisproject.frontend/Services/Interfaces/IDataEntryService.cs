using tesisproject.frontend.Models;
namespace tesisproject.frontend.Services.Interfaces
{
    public interface IDataEntryService
    {
        Task<(IReadOnlyList<ArticleDto> Preview, IReadOnlyList<string> Errors)> ValidateAndPreviewAsync(
        Stream fileStream,
        CancellationToken ct = default);

        Task<ImportResult> ImportAsync(IEnumerable<ArticleDto> items, CancellationToken ct = default);
    }
}
