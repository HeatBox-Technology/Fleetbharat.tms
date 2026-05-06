namespace FleetBharat.TMSService.Application.DTOs
{
    public class TripAlertDTO
    {
        public int OrgId { get; set; }
        public string? OrgName { get; set; }
        public string VehicleId { get; set; }
        public string VehicleNo { get; set; }
        public string DeviceNo { get; set; }
        public DateTime? GpsDate { get; set; }
        public string GeoId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? Address { get; set; }
        public string GeoName { get; set; }
        public string GeoStatus { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public int TripId { get; set; }
        public string? TripName { get; set; }
        public string? PointType { get; set; }
    }
}
