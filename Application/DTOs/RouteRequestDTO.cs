namespace FleetBharat.TMSService.Application.DTOs
{
    public class RouteRequestDTO
    {
        public int routeId { get; set; }
        public string routeName { get; set; }
        public string routePath { get; set; }
        public string routeType { get; set; }
        public int accountId { get; set; } 
        public int startGeoId { get; set; }
        public int endGeoId { get; set; }
        public string totalDistance { get; set; }
        public string totalTime { get; set; }
        public List<StopDetailsDTO> stopDetails { get; set; }
        public Guid? createdBy { get; set; }
        public bool isActive { get; set; }

    }
}
