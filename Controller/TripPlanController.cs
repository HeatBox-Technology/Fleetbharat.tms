using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Application.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FleetBharat.TMSService.Controller
{
    [ApiController]
    [Route("api/trip-plans")]
    public class TripPlanController : ControllerBase
    {
        private readonly ITripService _service;
        public TripPlanController(ITripService service)
        {
            _service = service;
        } 

        [HttpPost]
        public async Task<IActionResult> CreateTripPlan([FromBody] TripPlanRequestDTO request)
        {
            try
            {
                var response = await _service.CreateTripPlanAsync(request);
                return (Ok(response));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<int>.Fail(ex.Message, StatusCodes.Status500InternalServerError));
            }
            
        }

        [HttpGet("all/{accountId}")]
        public async Task<IActionResult> GetAllTripPlan(int accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var response = await _service.GetTripPlanListAsync(accountId, page, pageSize);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<int>.Fail(ex.Message, StatusCodes.Status500InternalServerError));
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteTripPlanAsync(id);

            return StatusCode(result.statusCode, result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateTripPlan([FromBody] TripPlanRequestDTO request)
        {
            var response = await _service.UpdateTripPlanAsync(request);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTripPlanById(int id)
        {
            var response = await _service.GetTripByIdAsync(id);
            return Ok(response);
        }
    }
}
