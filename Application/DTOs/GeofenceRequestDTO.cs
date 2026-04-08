namespace FleetBharat.TMSService.Application.DTOs
{
    public class GeofenceRequestDTO
    {
        public int accountId { get; set; }
        public string address { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
        public string displayName { get; set; }
        public int createdBy { get; set; }
    }

    public class GeofenceResponseDTO
    {
        public int geofenceId { get; set; }
    }
}
