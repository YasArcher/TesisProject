using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Convocation.Request;
using tesisproject.shared.DTOs.Convocation.Response;
using tesisproject.shared.DTOs.ConvocationRule.Request;
using tesisproject.shared.DTOs.ConvocationRule.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class ConvocationClientService : IConvocationClientService
    {
        private readonly IApiClient _api;

        public ConvocationClientService(IApiClient api)
        {
            _api = api;
        }

        // ========= CONVOCATIONS =========

        public Task<HttpResponseWrapper<List<ConvocationListItemResponseDTO>?>> GetListAsync(
            CancellationToken ct = default)
        {
            return _api.GetAsync<List<ConvocationListItemResponseDTO>?>(
                "api/convocations",
                ct);
        }

        public Task<HttpResponseWrapper<ConvocationDetailResponseDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            return _api.GetAsync<ConvocationDetailResponseDTO?>(
                $"api/convocations/{id}",
                ct);
        }

        public Task<HttpResponseWrapper<ConvocationDetailResponseDTO?>> CreateAsync(
            ConvocationCreateRequestDTO request,
            CancellationToken ct = default)
        {
            // TRequest = ConvocationCreateRequestDTO, TResponse = ConvocationDetailResponseDTO
            return _api.PostAsync<ConvocationCreateRequestDTO, ConvocationDetailResponseDTO>(
                "api/convocations",
                request,
                ct);
        }

        public Task<HttpResponseWrapper<ConvocationDetailResponseDTO?>> UpdateAsync(
            int id,
            ConvocationUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            // TRequest = ConvocationUpdateRequestDTO, TResponse = ConvocationDetailResponseDTO
            return _api.PutAsync<ConvocationUpdateRequestDTO, ConvocationDetailResponseDTO>(
                $"api/convocations/{id}",
                request,
                ct);
        }

        public Task<HttpResponseWrapper<NoContent?>> ActivateExclusiveAsync(
            int id,
            CancellationToken ct = default)
        {
            // POST: api/convocations/{id}/activate
            // TRequest = object, TResponse = NoContent
            return _api.PostAsync<object, NoContent?>(
                $"api/convocations/{id}/activate",
                new { },
                ct);
        }

        // ============= RULES =============

        public Task<HttpResponseWrapper<ConvocationRuleResponseDTO?>> AddRuleAsync(
            int convocationId,
            ConvocationRuleCreateRequestDTO request,
            CancellationToken ct = default)
        {
            // TRequest = ConvocationRuleCreateRequestDTO, TResponse = ConvocationRuleResponseDTO
            return _api.PostAsync<ConvocationRuleCreateRequestDTO, ConvocationRuleResponseDTO>(
                $"api/convocations/{convocationId}/rules",
                request,
                ct);
        }

        public Task<HttpResponseWrapper<ConvocationRuleResponseDTO?>> UpdateRuleAsync(
            int convocationId,
            int ruleId,
            ConvocationRuleUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            // TRequest = ConvocationRuleUpdateRequestDTO, TResponse = ConvocationRuleResponseDTO
            return _api.PutAsync<ConvocationRuleUpdateRequestDTO, ConvocationRuleResponseDTO>(
                $"api/convocations/{convocationId}/rules/{ruleId}",
                request,
                ct);
        }

        public Task<HttpResponseWrapper<NoContent?>> RemoveRuleAsync(
            int convocationId,
            int ruleId,
            CancellationToken ct = default)
        {
            return _api.DeleteAsync(
                $"api/convocations/{convocationId}/rules/{ruleId}",
                ct);
        }
    }
}
