using tesisproject.shared.DTOs.VisitIssues.Request;
using tesisproject.shared.DTOs.VisitIssues.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IVisitIssueClientService
    {
        // SINGLE
        Task<HttpResponseWrapper<VisitIssueResponseDTO?>> GetByIdAsync(int id, CancellationToken ct = default);

        // LIST BY VISIT
        Task<HttpResponseWrapper<List<VisitIssueResponseDTO>?>> ListByVisitAsync(int visitId, CancellationToken ct = default);

        // CREATE
        Task<HttpResponseWrapper<VisitIssueResponseDTO?>> CreateAsync(
            VisitIssueCreateRequestDTO request,
            CancellationToken ct = default);

        // UPDATE
        Task<HttpResponseWrapper<VisitIssueResponseDTO?>> UpdateAsync(
            int id,
            VisitIssueUpdateRequestDTO request,
            CancellationToken ct = default);

        // DELETE
        Task<HttpResponseWrapper<NoContent?>> DeleteAsync(int id, CancellationToken ct = default);
    }
}