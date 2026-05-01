namespace FleetBharat.TMSService.Application.DTOs
{
    public class GeofenceSyncDTO
    {
        public string geoId { get; set; }
        public string geoName { get; set; }
        public string pointType { get; set; }
        public decimal latitude { get; set; }
        public decimal longitude { get; set; }
        public int radius { get; set; }
        public string geoType { get; set; }
        public List<GeoPointSyncDTO> GeoPoints { get; set; }
    }
}
