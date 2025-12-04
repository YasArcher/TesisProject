using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Analytic.Interfaces
{
    /// <summary>
    /// Orchestrates DW ETL processes: dimensions, bridges, and facts.
    /// </summary>
    public interface IDwEtlService
    {
        /// <summary>
        /// Runs a full reload of the DW (dimensions, bridges, and facts)
        /// in the correct order.
        /// </summary>
        Task<ServiceResult<NoContent>> RunFullLoadAsync(CancellationToken ct = default);

        /// <summary>
        /// Loads or refreshes DW dimensions only.
        /// </summary>
        Task<ServiceResult<NoContent>> LoadDimensionsAsync(CancellationToken ct = default);

        /// <summary>
        /// Loads or refreshes DW bridge tables.
        /// </summary>
        Task<ServiceResult<NoContent>> LoadBridgesAsync(CancellationToken ct = default);

        /// <summary>
        /// Loads or refreshes DW fact tables.
        /// </summary>
        Task<ServiceResult<NoContent>> LoadFactsAsync(CancellationToken ct = default);
    }
}