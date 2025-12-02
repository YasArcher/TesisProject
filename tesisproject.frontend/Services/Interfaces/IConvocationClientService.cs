using tesisproject.shared.DTOs.Convocation.Request;
using tesisproject.shared.DTOs.Convocation.Response;
using tesisproject.shared.DTOs.ConvocationRule.Request;
using tesisproject.shared.DTOs.ConvocationRule.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IConvocationClientService
    {
        // ========= CONVOCATIONS =========

        // LIST (plano)
        Task<HttpResponseWrapper<List<ConvocationListItemResponseDTO>?>> GetListAsync(
            CancellationToken ct = default);

        // SINGLE (detail)
        Task<HttpResponseWrapper<ConvocationDetailResponseDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        // CREATE
        Task<HttpResponseWrapper<ConvocationDetailResponseDTO?>> CreateAsync(
            ConvocationCreateRequestDTO request,
            CancellationToken ct = default);

        // UPDATE
        Task<HttpResponseWrapper<ConvocationDetailResponseDTO?>> UpdateAsync(
            int id,
            ConvocationUpdateRequestDTO request,
            CancellationToken ct = default);

        // ACTIVATE EXCLUSIVE
        Task<HttpResponseWrapper<NoContent?>> ActivateExclusiveAsync(
            int id,
            CancellationToken ct = default);

        // ============= RULES =============

        Task<HttpResponseWrapper<ConvocationRuleResponseDTO?>> AddRuleAsync(
            int convocationId,
            ConvocationRuleCreateRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<ConvocationRuleResponseDTO?>> UpdateRuleAsync(
            int convocationId,
            int ruleId,
            ConvocationRuleUpdateRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<NoContent?>> RemoveRuleAsync(
            int convocationId,
            int ruleId,
            CancellationToken ct = default);
    }
}