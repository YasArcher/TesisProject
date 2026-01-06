// Frontend/Services/Interfaces/IVisitClientService.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.Visit.Request;
using tesisproject.shared.DTOs.Visit.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IVisitClientService
    {
        // LISTS (planos)
        Task<HttpResponseWrapper<List<VisitListResponseDTO>?>> GetListAsync(CancellationToken ct = default);
        Task<HttpResponseWrapper<List<VisitListResponseDTO>?>> GetByProjectAsync(int projectId, CancellationToken ct = default);
        // LISTS (Detail)
        Task<HttpResponseWrapper<VisitDetailResponseDTO?>> GetVisitDetailAsync(int id, CancellationToken ct = default);

        // SINGLE (plano)
        Task<HttpResponseWrapper<VisitListResponseDTO?>> GetByIdAsync(int id, CancellationToken ct = default);

        // UPDATE (plano)
        Task<HttpResponseWrapper<VisitListResponseDTO?>> UpdateAsync(UpdateVisitRequestDTO request, CancellationToken ct = default);
        Task<HttpResponseWrapper<VisitListResponseDTO?>> FinalizeAsync(int visitId, FinalizeVisitRequestDTO request, CancellationToken ct = default);
        Task<HttpResponseWrapper<NoContent?>> DeleteAsync(int id, CancellationToken ct = default);
        Task<HttpResponseWrapper<VisitListResponseDTO?>> CreateAsync(AddVisitRequestDTO request, CancellationToken ct = default);
    }
}
