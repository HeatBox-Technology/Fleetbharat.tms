namespace FleetBharat.TMSService.Application.DTOs
{
    public class GeofenceRawDTO
    {
        public int sequence { get; set; }
        public string pointType { get; set; }
        public int geofenceId { get; set; }
        public string geofenceType { get; set; }
        public string geofenceRadius { get; set; }
        public string geofenceCenterLatitude { get; set; }
        public string geofenceCenterLongitude { get; set; }
        public string geofenceDetails { get; set; } // for polygon (future use)
    }
}
