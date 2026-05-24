using System;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Venues;

namespace tesisproject.frontend.Services.Implementations
{
    public sealed class VenuesClient : IVenuesClient
    {
        private const string VenuesBasePath = "/api/venues";

        private readonly IApiClient _api;

        public VenuesClient(IApiClient api)
            => _api = api ?? throw new ArgumentNullException(nameof(api));

        /// <inheritdoc />
        public Task<VenueUpsertResponse?> UpsertVenueAsync(
            VenueUpsertRequest req,
            CancellationToken ct = default
        )
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            return _api.PostAsync<VenueUpsertRequest, VenueUpsertResponse>(
                VenuesBasePath,
                req,
                ct
            );
        }
        public Task<VenueMetricUpsertResponse?> UpsertMetricAsync(
            int venueId,
            VenueMetricUpsertRequest req,
            CancellationToken ct = default
        )
        {
            if (venueId <= 0) throw new ArgumentOutOfRangeException(nameof(venueId));
            if (req is null) throw new ArgumentNullException(nameof(req));

            // POST /api/venues/{venueId}/metrics
            // Ajusta a la ruta real si tu backend usa /metrics/upsert.
            var path = $"{VenuesBasePath}/{venueId}/metrics";

            return _api.PostAsync<VenueMetricUpsertRequest, VenueMetricUpsertResponse>(
                path,
                req,
                ct
            );
        }
    }
}
