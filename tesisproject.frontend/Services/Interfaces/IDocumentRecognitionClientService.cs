using tesisproject.shared.DTOs.Algorithms.Response;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IDocumentRecognitionClientService
    {
        Task<HttpResponseWrapper<ResolutionInfo?>> AnalyzeResolutionAsync(
            MultipartFormDataContent content,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<DideProjectFormInfo?>> AnalyzeDideProjectAsync(
            MultipartFormDataContent content,
            CancellationToken ct = default);
    }
}
