using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.frontend.Services;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Algorithms.Response;

namespace tesisproject.frontend.Services.Implementations
{
    public class DocumentRecognitionClientService : IDocumentRecognitionClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "documentrecognition";

        public DocumentRecognitionClientService(IApiClient api)
        {
            _api = api;
        }

        // =======================================================
        //                 ANALYZE RESOLUTION (PDF)
        // =======================================================
        public Task<HttpResponseWrapper<ResolutionInfo?>> AnalyzeResolutionAsync(
            MultipartFormDataContent content,
            CancellationToken ct = default)
        {
            // POST: api/documentrecognition/analyze-resolution
            return _api.PostMultipartAsync<ResolutionInfo>(
                $"{_baseUrl}/analyze-resolution",
                content,
                ct
            );
        }

        // =======================================================
        //              ANALYZE DIDE PROJECT (PDF)
        // =======================================================
        public Task<HttpResponseWrapper<DideProjectFormInfo?>> AnalyzeDideProjectAsync(
            MultipartFormDataContent content,
            CancellationToken ct = default)
        {
            // POST: api/documentrecognition/analyze-dide-project
            return _api.PostMultipartAsync<DideProjectFormInfo>(
                $"{_baseUrl}/analyze-dide-project",
                content,
                ct
            );
        }
    }
}
