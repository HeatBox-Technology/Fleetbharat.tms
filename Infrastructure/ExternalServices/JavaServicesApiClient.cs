using FleetBharat.TMSService.Application.DTOs;

namespace FleetBharat.TMSService.Infrastructure.ExternalServices
{
    public class JavaServicesApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public JavaServicesApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> SendTripMappingAsync(
        List<TripSyncDTO> trips,
        CancellationToken ct = default)
        {
            var client = _httpClientFactory.CreateClient("TripMappingClient");

            var response = await client.PostAsJsonAsync(
                "api/v1/mapping/trip-mapping",
                trips,
                ct
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new Exception($"Trip Mapping API failed: {response.StatusCode}, {error}");
            }

            return true;
        }
    }
}
