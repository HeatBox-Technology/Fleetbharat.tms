namespace FleetBharat.TMSService.Application.DTOs
{
    public class TripPlanRouteDetailsDTO
    {
        public int fromGeoId { get; set; }
        public int toGeoId { get; set; }
        public int sequence { get; set; }
        public string distance { get; set; }
        public int rta { get; set; }
        public int leadTime { get; set; }
        public string toEntryTime { get; set; }
        public string fromExitTime { get; set; }
        public int googleSuggestedTime { get; set; }
    }
}
