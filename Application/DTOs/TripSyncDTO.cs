namespace FleetBharat.TMSService.Application.DTOs
{
    public class TripSyncDTO
    {
        public int tripId { get; set; }
        public string tripName { get; set; }
        public string vehicleId { get; set; }
        public string vehicleNo { get; set; }
        public string deviceNo { get; set; }
        public int orgId { get; set; }
        public string orgName { get; set; }
        public List<string> secondaryDevices { get; set; }
        public DateTime tripStartTime { get; set; }
        public DateTime tripEndTime { get; set; }
        public string encodedRoute { get; set; }
        public List<GeofenceSyncDTO> geofenceList { get; set; }
    }
}
