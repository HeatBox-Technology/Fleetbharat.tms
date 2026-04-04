namespace FleetBharat.TMSService.Application.DTOs
{
    public class TripPlanRouteDetailsDTO
    {
        public int fromGeoId { get; set; }
        public string? fromGeoName { get; set; }
        public string? fromLatitude { get; set; }
        public string? fromLongitude { get; set; }
        public int toGeoId { get; set; }
        public string? toGeoName { get; set; }
        public string? toLatitude { get; set; }
        public string? toLongitude { get; set; }
        public int sequence { get; set; }
        public string distance { get; set; }
        public int leadTime { get; set; }
        public int rta { get; set; }
    }
}
