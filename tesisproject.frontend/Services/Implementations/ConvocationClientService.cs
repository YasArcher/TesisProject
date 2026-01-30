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
        private const string BaseUrl = "convocations";

        public ConvocationClientService(IApiClient api)
        {
            _api = api;
        }

        // ========= CONVOCATIONS =========

        public Task<HttpResponseWrapper<List<ConvocationListItemResponseDTO>?>> GetListAsync(
            CancellationToken ct = default)
        {
            return _api.GetAsync<List<ConvocationListItemResponseDTO>?>(
                BaseUrl,
                ct);
        }

        public Task<HttpResponseWrapper<ConvocationDetailResponseDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            return _api.GetAsync<ConvocationDetailResponseDTO?>(
                $"{BaseUrl}/{id}",
                ct);
        }

        public Task<HttpResponseWrapper<ConvocationDetailResponseDTO?>> CreateAsync(
            ConvocationCreateRequestDTO request,
            CancellationToken ct = default)
        {
            return _api.PostAsync<ConvocationCreateRequestDTO, ConvocationDetailResponseDTO>(
                BaseUrl,
                request,
                ct);
        }

        public Task<HttpResponseWrapper<ConvocationDetailResponseDTO?>> UpdateAsync(
            int id,
            ConvocationUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            return _api.PutAsync<ConvocationUpdateRequestDTO, ConvocationDetailResponseDTO>(
                $"{BaseUrl}/{id}",
                request,
                ct);
        }

        public Task<HttpResponseWrapper<NoContent?>> ActivateExclusiveAsync(
            int id,
            CancellationToken ct = default)
        {
            // POST: convocations/{id}/activate
            return _api.PostAsync<object, NoContent?>(
                $"{BaseUrl}/{id}/activate",
                new { },
                ct);
        }

        // ============= RULES =============

        public Task<HttpResponseWrapper<ConvocationRuleResponseDTO?>> AddRuleAsync(
            int convocationId,
            ConvocationRuleCreateRequestDTO request,
            CancellationToken ct = default)
        {
            // POST: convocations/{convocationId}/rules
            return _api.PostAsync<ConvocationRuleCreateRequestDTO, ConvocationRuleResponseDTO>(
                $"{BaseUrl}/{convocationId}/rules",
                request,
                ct);
        }

        public Task<HttpResponseWrapper<ConvocationRuleResponseDTO?>> UpdateRuleAsync(
            int convocationId,
            int ruleId,
            ConvocationRuleUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            // PUT: convocations/{convocationId}/rules/{ruleId}
            return _api.PutAsync<ConvocationRuleUpdateRequestDTO, ConvocationRuleResponseDTO>(
                $"{BaseUrl}/{convocationId}/rules/{ruleId}",
                request,
                ct);
        }

        public Task<HttpResponseWrapper<NoContent?>> RemoveRuleAsync(
            int convocationId,
            int ruleId,
            CancellationToken ct = default)
        {
            // DELETE: convocations/{convocationId}/rules/{ruleId}
            return _api.DeleteAsync(
                $"{BaseUrl}/{convocationId}/rules/{ruleId}",
                ct);
        }
    }
}