namespace FleetBharat.TMSService.Application.DTOs
{
    public class RouteResponseDTO
    {
        public int routeId { get; set; }
        public string routeName { get; set; }
        public string routePath { get; set; }
        public string routeType { get; set; }
        public int accountId { get; set; }
        // Resolved account name (populated from Common API)
        public string? accountName { get; set; }
        public int startGeoId { get; set; }
        public int endGeoId { get; set; }
        // Resolved geofence names (populated by services when available)
        public string? startGeoName { get; set; }
        public string? endGeoName { get; set; }
        public string totalDistance { get; set; }
        public string totalTime { get; set; }
        public List<StopDetailsDTO> stopDetails { get; set; }
        // Number of stops for the route
        public int stopCount { get; set; }
        public Guid? createdBy { get; set; }
        public bool isActive { get; set; }
    }
}
