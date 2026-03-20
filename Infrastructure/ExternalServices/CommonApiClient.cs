using System.Net.Http.Headers;
using System.Text.Json;
using FleetBharat.TMSService.Application.DTOs;

namespace Infrastructure.ExternalServices
{
    public class CommonApiClient
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public CommonApiClient(HttpClient http)
        {
            _http = http;
        }



        /// <summary>
        /// Calls external API GET /api/common/dropdowns/geofences with accountId and limit and returns parsed JSON document.
        /// Authorization header is attached by the client's message handler (e.g. AuthHeaderHandler); do not pass the token here.
        /// </summary>
        public async Task<List<CommonResponseDTO>> GetGeofencesAsync(int accountId, int limit = 200, CancellationToken cancellationToken = default)
        {
            var uri = $"api/common/dropdowns/geofences?accountId={accountId}&limit={limit}";
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);

            var envelope = await JsonSerializer.DeserializeAsync<
                FleetBharat.TMSService.Application.DTOs.CommonApiResponse<CommonResponseDTO>>(
                stream, _jsonOptions, cancellationToken);

            // Return the typed data list or an empty list
            return envelope?.data ?? new List<CommonResponseDTO>();
        }

        /// <summary>
        /// Calls external API GET /api/common/dropdowns/accounts and returns list of id/value items.
        /// </summary>
        public async Task<List<CommonResponseDTO>> GetAccountsAsync(int limit = 200, CancellationToken cancellationToken = default)
        {
            var uri = $"api/common/dropdowns/accounts?limit={limit}";
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);

            var envelope = await JsonSerializer.DeserializeAsync<
                FleetBharat.TMSService.Application.DTOs.CommonApiResponse<CommonResponseDTO>>(
                stream, _jsonOptions, cancellationToken);

            return envelope?.data ?? new List<CommonResponseDTO>();
        }


        /// <summary>
        /// Calls external API GET /api/common/dropdowns/accounts and returns list of id/value items.
        /// </summary>
        public async Task<List<CommonResponseDTO>> GetVehicleAsync(int accountId, CancellationToken cancellationToken = default)
        {
            var uri = $"api/common/dropdowns/vehicles/{accountId}";
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);

            var envelope = await JsonSerializer.DeserializeAsync<
                FleetBharat.TMSService.Application.DTOs.CommonApiResponse<CommonResponseDTO>>(
                stream, _jsonOptions, cancellationToken);

            return envelope?.data ?? new List<CommonResponseDTO>();
        }
    }
}
