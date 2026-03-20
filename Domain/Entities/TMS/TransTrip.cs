using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetBharat.TMSService.Domain.Entities.TMS
{
    [Table("Trans_Trip", Schema = "TMS")]
    public class TransTrip
    {
        [Key]
        [Column("trip_id")]
        public int TripId { get; set; }

        [Column("account_id")]
        public int AccountId { get; set; }

        [Column("driver_id")]
        public int DriverId { get; set; }

        [Column("vehicle_id")]
        public int VehicleId { get; set; }

        [Column("trip_type")]
        public string TripType { get; set; }

        [Column("travel_date", TypeName = "timestamp")]
        public DateTime? TravelDate { get; set; }

        [Column("etd", TypeName = "timestamp")]
        public DateTime ETD { get; set; }

        [Column("rta", TypeName = "timestamp")]
        public DateTime RTA { get; set; }

        [Column("total_lead_time")]
        public int TotalLeadTime { get; set; }

        [Column("route_id")]
        public int RouteId { get; set; }

        [Column("created_datetime", TypeName = "timestamp")]
        public DateTime CreatedDatetime { get; set; } = DateTime.UtcNow;

        [Column("created_by")]
        public Guid CreatedBy { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("driver_name")]
        public string DriverName { get; set; }

        [Column("vehicle_no")]
        public string VehicleNo { get; set; }

        [Column("driver_phone")]
        public string DriverPhone { get; set; }

        [Column("start_geo_id")]
        public int StartGeoId { get; set; }

        [Column("end_geo_id")]
        public int EndGeoId { get; set; }

        [Column("is_elock")]
        public bool IsElock { get; set; }

        [Column("is_gps")]
        public bool IsGPS { get; set; }

        [Column("primary_device")]
        public string? PrimaryDevice { get; set; }

        [Column("consignee")]
        public int? Consignee { get; set; }

        [Column("consignor")]
        public int? Consignor { get; set; }
    }
}
