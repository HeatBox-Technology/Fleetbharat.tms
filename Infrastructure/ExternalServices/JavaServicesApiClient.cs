using FleetBharat.TMSService.Application.DTOs;
using Newtonsoft.Json;
using System.Text;

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

            var json = JsonConvert.SerializeObject(trips);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(
                "api/v1/mapping/trip-mapping",
                content,
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
