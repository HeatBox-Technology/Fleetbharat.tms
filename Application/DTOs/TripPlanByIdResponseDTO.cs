namespace FleetBharat.TMSService.Application.DTOs
{
    public class TripPlanByIdResponseDTO
    {
        
        public int planId { get; set; }
        public int accountId { get; set; }
        public int? driverId { get; set; }
        public int? vehicleId { get; set; }
        public string? frequency { get; set; } // "recurring" or "one-time"
        public DateTime? travel_date { get; set; }
        public string? travelDate { get; set; }
        public int? routeId { get; set; }
        public int? startGeoId { get; set; }
        public int? endGeoId { get; set; }
        public string? weekDays { get; set; }
        public DateTime createdDatetime { get; set; }
        public Guid createdBy { get; set; }
        public string? driverName { get; set; }
        public string? vehicleNo { get; set; }
        public string? driverPhone { get; set; }
        public string? routingModel { get; set; }
        public string? fleetSource { get; set; }
        public string? routePath { get; set; }
        public bool isElockTrip { get; set; }
        public bool isGPSTrip { get; set; }
        public string? primaryDevice { get; set; }
        public List<string?> secondaryDevice { get; set; }
        public string? secondaryDeviceJson { get; set; }
        public int? consignee { get; set; }
        public int? consignor { get; set; }
        public string? vehicleCategory { get; set; }
        // Nested Route Details
        public List<TripPlanGeofenceRouteDetailsDTO> routeDetails { get; set; }
        
    }
}
