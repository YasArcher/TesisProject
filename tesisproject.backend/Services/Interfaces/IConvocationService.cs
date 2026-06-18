using tesisproject.shared.DTOs.Convocation.Request;
using tesisproject.shared.DTOs.Convocation.Response;
using tesisproject.shared.DTOs.ConvocationRule.Request;
using tesisproject.shared.DTOs.ConvocationRule.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IConvocationService
    {
        // ================= CONVOCATIONS =================
        Task<ServiceResult<ConvocationDetailResponseDTO>> CreateAsync(ConvocationCreateRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<ConvocationDetailResponseDTO>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ServiceResult<IReadOnlyList<ConvocationListItemResponseDTO>>> ListAsync(CancellationToken ct = default);
        Task<ServiceResult<ConvocationDetailResponseDTO>> UpdateAsync(ConvocationUpdateRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<NoContent>> ActivateExclusiveAsync(int id, CancellationToken ct = default);

        // ================= RULES =================
        Task<ServiceResult<ConvocationRuleResponseDTO>> AddRuleAsync(ConvocationRuleCreateRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<ConvocationRuleResponseDTO>> UpdateRuleAsync(ConvocationRuleUpdateRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<NoContent>> RemoveRuleAsync(int convocationId, int ruleId, CancellationToken ct = default);
    }
}