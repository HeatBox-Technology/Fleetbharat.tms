using FleetBharat.TMSService.Application.DTOs;

namespace FleetBharat.TMSService.Application.Interfaces
{
    public interface IRouteService
    {
        Task<RouteRequestDTO> CreateOrUpdateAsync(RouteRequestDTO route);

        Task<RouteRequestDTO?> GetRouteAsync(int id);
        
        Task<RouteListUiResponseDto> GetRoutesByAccountAsync(int accountId, int page = 1, int pageSize = 20, string? search = null);

        Task<ApiResponse<string>> DeleteRouteAsync(int routeId);

        Task<ApiResponse<List<DropdownDto>>> GetRouteDropdown(int accountId);
        
    }
}
