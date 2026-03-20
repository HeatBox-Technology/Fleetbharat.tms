using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/trip")]
public class GetController : ControllerBase
{
    private readonly GetServices _getServices;

    public GetController(GetServices getServices)
    {
        _getServices = getServices;
    }
    [HttpGet("vehicles")]
    public async Task<IActionResult> Vehicles()
    {
        var accountClaim = User.FindFirst("accountId");

        if (accountClaim == null)
            return Unauthorized(ApiResponse<object>.Fail("AccountId missing in token", 401));

        if (!int.TryParse(accountClaim.Value, out int accountId))
            return Unauthorized(ApiResponse<object>.Fail("Invalid AccountId", 401));

        var data = await _getServices.GetVehiclesAsync(accountId);

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }
}