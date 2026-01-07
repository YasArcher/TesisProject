using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IDwEtlClientService
    {
        /// <summary>
        /// Runs the full DW ETL on demand.
        /// </summary>
        Task<HttpResponseWrapper<NoContent>> RunFullAsync(CancellationToken ct = default);
    }
}