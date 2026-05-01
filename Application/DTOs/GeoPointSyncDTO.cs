using FleetBharat.TMSService.Application.Filters;
using Newtonsoft.Json;

namespace FleetBharat.TMSService.Application.DTOs
{
    public class GeoPointSyncDTO
    {
        [JsonConverter(typeof(SafeDecimalConverter))]
        public decimal latitude { get; set; }

        [JsonConverter(typeof(SafeDecimalConverter))]
        public decimal longitude { get; set; }
    }
}
