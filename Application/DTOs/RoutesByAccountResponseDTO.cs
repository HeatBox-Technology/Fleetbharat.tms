using System.Collections.Generic;

namespace FleetBharat.TMSService.Application.DTOs
{
    public class RoutesByAccountResponseDTO
    {
        public List<RouteResponseDTO> routes { get; set; } = new List<RouteResponseDTO>();
        public int totalRoutes { get; set; }
        public int totalActiveRoutes { get; set; }

        public int totalInactiveRoutes { get; set; }
    }
}
