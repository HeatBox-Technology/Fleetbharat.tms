namespace FleetBharat.TMSService.Application.DTOs
{
    public class TripPlanRequestDTO
    {
        public int planId { get; set; }
        public int accountId { get; set; }
        public int driverId { get; set; }
        public string? driverName { get; set; }
        public string? driverPhone { get; set; }
        public int vehicleId { get; set; }
        public string? vehicleNumber { get; set; }
        public string tripType { get; set; }
        public string? travelDate { get; set; }
        public string etd { get; set; }
        public int routeId { get; set; }
        public int startGeoId { get; set; }
        public int endGeoId { get; set; }
        public Guid createdBy { get; set; }
        public string weekDays { get; set; }
        public bool isElockTrip { get; set; }
        public bool isGPSTrip { get; set; }
        public string? primaryDevice { get; set; }
        public int? Consignee { get; set; }
        public int? Consignor { get; set; }
        public List<TripPlanRouteDetailsDTO> routeDetails { get; set; }
    }
}
