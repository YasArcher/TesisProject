using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.shared.DTOs;
using tesisproject.shared.DTOs.Venues;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IVenuesService
    {
        // VENUE
        Task<PagedResult<VenueDto>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct = default);
        Task<VenueDto?> GetByIdAsync(int venueId, CancellationToken ct = default);
        Task<VenueUpsertResponse> UpsertVenueAsync(VenueUpsertRequest req, CancellationToken ct = default);
        Task<bool> DeleteVenueAsync(int venueId, CancellationToken ct = default);

        // METRICS
        Task<IReadOnlyList<VenueMetricDto>> GetMetricsAsync(int venueId, CancellationToken ct = default);
        Task<VenueMetricUpsertResponse> UpsertMetricAsync(int venueId, VenueMetricUpsertRequest req, CancellationToken ct = default);
        Task<bool> DeleteMetricAsync(int venueId, short year, CancellationToken ct = default);
    }
}
