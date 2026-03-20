using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

public class IdentityApiClient
{
    private readonly HttpClient _http;

    public IdentityApiClient(HttpClient http)
    {
        _http = http;
    }

    private static readonly JsonSerializerOptions _jsonOptions =
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

    // ------------------ GET ------------------

    public async Task<T?> GetAsync<T>(string url) where T : class
    {
        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();

        var apiResponse =
            JsonSerializer.Deserialize<ApiResponse<T>>(json, _jsonOptions);

        if (apiResponse == null || !apiResponse.success)
            return null;

        return apiResponse.data;
    }

    // ------------------ POST ------------------

    public async Task<T?> PostAsync<T>(string url, object body) where T : class
    {
        var response = await _http.PostAsJsonAsync(url, body);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();

        var apiResponse =
            JsonSerializer.Deserialize<ApiResponse<T>>(json, _jsonOptions);

        if (apiResponse == null || !apiResponse.success)
            return null;

        return apiResponse.data;
    }

    // ------------------ PUT (UPDATE) ------------------

    public async Task<T?> PutAsync<T>(string url, object body) where T : class
    {
        var response = await _http.PutAsJsonAsync(url, body);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();

        var apiResponse =
            JsonSerializer.Deserialize<ApiResponse<T>>(json, _jsonOptions);

        if (apiResponse == null || !apiResponse.success)
            return null;

        return apiResponse.data;
    }

    // ------------------ DELETE ------------------

    public async Task<bool> DeleteAsync(string url)
    {
        var response = await _http.DeleteAsync(url);

        if (!response.IsSuccessStatusCode)
            return false;

        var json = await response.Content.ReadAsStringAsync();

        var apiResponse =
            JsonSerializer.Deserialize<ApiResponse<object>>(json, _jsonOptions);

        return apiResponse != null && apiResponse.success;
    }
}