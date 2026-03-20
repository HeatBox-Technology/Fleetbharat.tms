using FleetBharat.TMSService.Application.DTOs;
using System.Collections.Generic;
using FleetBharat.TMSService.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FleetBharat.TMSService.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteController : ControllerBase
    {
        private readonly IRouteService _routeService;

        public RouteController(IRouteService routeService)
        {
            _routeService = routeService;
        }

        // Create or update a route along with its stops
        [HttpPost]
        public async Task<IActionResult> CreateRoute([FromBody] RouteRequestDTO route)
        {
            try
            {
                var result = await _routeService.CreateOrUpdateAsync(route);
                return Ok(ApiResponse<RouteRequestDTO>.Ok(result));
            }
            catch (ArgumentNullException ane)
            {
                return BadRequest(ApiResponse<RouteRequestDTO>.Fail(ane.Message, StatusCodes.Status400BadRequest));
            }
            catch (ArgumentException aex)
            {
                return BadRequest(ApiResponse<RouteRequestDTO>.Fail(aex.Message, StatusCodes.Status400BadRequest));
            }
            catch (KeyNotFoundException knf)
            {
                return NotFound(ApiResponse<RouteRequestDTO>.Fail(knf.Message, StatusCodes.Status404NotFound));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<RouteRequestDTO>.Fail(ex.Message, StatusCodes.Status500InternalServerError));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoute(int id)
        {
            try
            {
                var result = await _routeService.GetRouteAsync(id);
                if (result == null)
                    return NotFound(ApiResponse<RouteRequestDTO>.Fail("Not Found", StatusCodes.Status404NotFound));

                return Ok(ApiResponse<RouteRequestDTO>.Ok(result));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<RouteRequestDTO>.Fail(ex.Message, StatusCodes.Status500InternalServerError));
            }
        }

        [HttpGet("all/{accountId}")]
        public async Task<IActionResult> GetRoutesByAccount(int accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _routeService.GetRoutesByAccountAsync(accountId, page, pageSize);
                return Ok(ApiResponse<RouteListUiResponseDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<RouteListUiResponseDto>.Fail(ex.Message, StatusCodes.Status500InternalServerError));
            }
        }

        [HttpGet("dropdown/{accountId}")]
        public async Task<IActionResult> GetRouteDropdown(int accountId)
        {
            var response = await _routeService.GetRouteDropdown(accountId);
            return StatusCode(response.statusCode, response);
        }

     }
}
