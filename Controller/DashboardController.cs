using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FleetBharat.TMSService.Controller
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("trip")]
        public async Task<IActionResult> GetTripDashboard()
        {
            var dashboard = await _dashboardService.GetTripDashboardAsync();
            return Ok(ApiResponse<TripDashboardDto>.Ok(dashboard, "Trip dashboard data retrieved successfully."));
        }
    }
}
