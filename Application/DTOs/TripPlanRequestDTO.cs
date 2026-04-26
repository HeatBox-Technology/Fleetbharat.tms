namespace FleetBharat.TMSService.Application.DTOs
{
    public class TripPlanRequestDTO
    {
        public int planId { get; set; }
        public int accountId { get; set; }
        public int driverId { get; set; }
        public string? driverName { get; set; }
        public string? driverPhone { get; set; }
        public string fleetSource { get; set; } //Internal/External
        public int vehicleId { get; set; }
        public string? vehicleNumber { get; set; }
        public string frequency { get; set; } //one-time/recurring
        public string? travelDate { get; set; }
        public string routingModel { get; set; } //standard/dynamic
        public int routeId { get; set; }
        public string? routePath { get; set; }
        public int startGeoId { get; set; }
        public int endGeoId { get; set; }
        public Guid createdBy { get; set; }
        public string weekDays { get; set; }
        //public bool isElockTrip { get; set; }
        //public bool isGPSTrip { get; set; }
        public string tripType { get; set; } //Elock/GPS
        public string? primaryDevice { get; set; }
        public List<string>? secondaryDevice { get; set; }
        public string? vehicleCategory { get; set; }
        public int? Consignee { get; set; }
        public int? Consignor { get; set; }
        public List<TripPlanGeofenceRouteDetailsDTO> routeDetails { get; set; }
    }
}
