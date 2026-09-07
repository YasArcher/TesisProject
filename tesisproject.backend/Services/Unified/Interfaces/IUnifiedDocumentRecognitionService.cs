using tesisproject.shared.DTOs.Algorithms.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedDocumentRecognitionService
    {
        Task<ServiceResult<ResolutionInfo>> RecognizeResolutionAsync(
            IFormFile file,
            CancellationToken ct = default);

        Task<ServiceResult<DideProjectFormInfo>> RecognizeDideProjectAsync(
            IFormFile file,
            CancellationToken ct = default);
    }
}