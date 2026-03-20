using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace FleetBharat.TMSService.Domain.Entities.TMS
{
    [Table("mst_route",Schema ="TMS")]
    public class RouteMaster
    {
        [Key]
        [Column("route_id")]
        public int RouteId { get; set; }

        [Column("route_name")]
        public string? RouteName { get; set; }

        [Column("route_path")]
        public string? RoutePath { get; set; }

        [Column("route_type")]
        public string? RouteType { get; set; }

        [Column("account_id")]
        public int AccountId { get; set; }

        [Column("start_geo_id")]
        public int StartGeoId { get; set; }

        [Column("end_geo_id")]
        public int EndGeoId { get; set; }

        [Column("total_distance")]
        public string? TotalDistance { get; set; }

        [Column("total_time")]
        public string? TotalTime { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_by")]
        public Guid? CreatedBy { get; set; }

        [Column("created_datetime")]
        public DateTime? CreatedDatetime { get; set; }

        [Column("updated_by")]
        public Guid? UpdatedBy { get; set; }

        [Column("updated_datetime")]
        public DateTime? UpdatedDatetime { get; set; }

    }
}
