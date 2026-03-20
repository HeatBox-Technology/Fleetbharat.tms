namespace FleetBharat.TMSService.Application.DTOs
{
    public class StopDetailsDTO
    {
        public int sequence { get; set; }
        public int fromGeoId { get; set; }
        public int toGeoId { get; set; }
        public string? distance { get; set; }
        public string? time { get; set; }

    }
}
