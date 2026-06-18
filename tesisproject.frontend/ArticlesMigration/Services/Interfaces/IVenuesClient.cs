using System.Threading;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.Venues;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IVenuesClient
    {
        Task<VenueUpsertResponse?> UpsertVenueAsync(
            VenueUpsertRequest req,
            CancellationToken ct = default
        );

        Task<VenueMetricUpsertResponse?> UpsertMetricAsync(
            int venueId,
            VenueMetricUpsertRequest req,
            CancellationToken ct = default
        );
    }
}
