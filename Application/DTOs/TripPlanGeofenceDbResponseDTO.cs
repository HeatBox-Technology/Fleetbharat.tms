namespace FleetBharat.TMSService.Application.DTOs
{
    public class TripPlanGeofenceDbResponseDTO
    {
        public int geofenceId { get; set; }
        public string? geofenceType { get; set; }
        public string? pointType { get; set; }
        public string? geofenceAddress { get; set; }
        public string? geofenceCenterLatitude { get; set; }
        public string? geofenceCenterLongitude { get; set; }
        public string? geofenceRadius { get; set; }
        public string? plannedEntryTime { get; set; }
        public string? plannedExitTime { get; set; }
        public int sequence { get; set; }
        public string? distance { get; set; }
        public int googleSuggestedTime { get; set; }
        public string? geofenceDetails { get; set; }
    }
}
