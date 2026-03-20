namespace FleetBharat.TMSService.Application.DTOs
{
    public class RouteSummaryDto
    {
        public int TotalRoutes { get; set; }
        public int TotalActiveRoutes { get; set; }
        public int TotalInactiveRoutes { get; set; }
    }

    public class RouteListUiResponseDto
    {
        public RouteSummaryDto Summary { get; set; } = new();

        public PagedResultDto<RouteResponseDTO> Assignments { get; set; } = new();
    }
}
