using tesisproject.shared.DTOs.VisitIssues.Request;
using tesisproject.shared.DTOs.VisitIssues.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IVisitIssueService
    {
        Task<ServiceResult<VisitIssueResponseDTO>> CreateAsync(
            VisitIssueCreateRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<VisitIssueResponseDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<IReadOnlyList<VisitIssueResponseDTO>>> ListByVisitAsync(
            int visitId,
            CancellationToken ct = default);

        Task<ServiceResult<VisitIssueResponseDTO>> UpdateAsync(
            int id,
            VisitIssueUpdateRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<NoContent>> DeleteAsync(
            int id,
            CancellationToken ct = default);
    }
}
