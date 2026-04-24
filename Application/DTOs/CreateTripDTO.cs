namespace FleetBharat.TMSService.Application.DTOs
{
    public class CreateTripDTO
    {
        public int planId { get; set; }
        public int accountId { get; set; }
        public int vehicleId { get; set; } = 0;
        public string? vehicleNumber { get; set; }
        public int driverId { get; set; } = 0; 
        public string? driverName { get; set; }
        public string? primaryDevice { get; set; }
        public string frequency { get; set; }
        public string? weekDays { get; set; }
        public int? routeId { get; set; }
        public int startGeoId { get; set; }
        public int endGeoId { get; set; }
        public string? driverPhone { get; set; }
        public string plannedStartTime { get; set; }
        public string plannedEndTime { get; set; }
        public int googleSuggestedTime { get;set; }
        public string? secondaryDevices { get; set; }
        public bool isElockTrip { get; set; }
        public bool isGPSTrip { get; set; }
        public int? Consignee { get; set; }
        public int? Consignor { get; set; }
        public string? vehicleCategory { get; set; }
        public string? routePath { get; set; }
        public string? routingModel { get; set; }
        public Guid? createdBy { get; set; }
    }
}
