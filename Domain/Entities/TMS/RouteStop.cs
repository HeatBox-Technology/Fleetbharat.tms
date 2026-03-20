using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetBharat.TMSService.Domain.Entities.TMS
{
    [Table("mst_route_stop",Schema ="TMS")]
    public class RouteStop
    {
        [Key]
        [Column("route_stop_id")]
        public int RouteStopId { get; set; }

        [Column("route_id")]
        public int RouteId { get; set; }

        // sequence in the route
        [Column("sequence")]
        public int Sequence { get; set; }

        [Column("from_geo_id")]
        public int FromGeoId { get; set; }

        [Column("to_geo_id")]
        public int ToGeoId { get; set; }

        [Column("distance")]
        public string? Distance { get; set; }

        [Column("time")]
        public string? Time { get; set; }

        
    }
}
