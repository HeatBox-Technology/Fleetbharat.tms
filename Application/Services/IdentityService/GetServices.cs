public class GetServices
{
    private readonly IdentityApiClient _client;

    public GetServices(IdentityApiClient client)
    {
        _client = client;
    }

    public async Task<List<VehicleDto>> GetVehiclesAsync(int accountId)
    {
        var result = await _client.GetAsync<List<VehicleDto>>(
            $"{IdentityRoutes.Vehicles}/{accountId}");

        return result ?? new List<VehicleDto>();
    }
}