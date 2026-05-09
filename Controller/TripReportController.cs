using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FleetBharat.TMSService.Controller
{
    [ApiController]
    [Route("api/trip-report")]
    public class TripReportController : ControllerBase
    {
        private readonly ITripReportService _tripReportService;

        public TripReportController(ITripReportService tripReportService)
        {
            _tripReportService = tripReportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTripReport([FromQuery] TripReportFilterDto request)
        {
            var response = await _tripReportService.GetTripReportAsync(request);
            return StatusCode(response.statusCode, response);
        }
    }
}
