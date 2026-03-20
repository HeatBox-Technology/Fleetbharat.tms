using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetBharat.TMSService.Domain.Entities.TMS
{
    [Table("Trip_Plan", Schema = "TMS")]
    public class TripPlan
    {
        [Key]
        [Column("plan_id")]
        public int PlanId { get; set; }

        [Column("account_id")]
        public int AccountId { get; set; }

        [Column("driver_id")]
        public int DriverId { get; set; }

        [Column("vehicle_id")]
        public int VehicleId { get; set; }

        [Column("trip_type")]
        public string TripType { get; set; }

        [Column("ETD")]
        public string ETD { get; set; }

        [Column("lead_time")]
        public int LeadTime { get; set; }

        [Column("ETA")]
        public int ETA { get; set; }

        [Column("travel_date")]
        public string? TravelDate { get; set; }

        [Column("route_id")]
        public int RouteId { get; set; }

        [Column("created_datetime")]
        public DateTime CreatedDatetime { get; set; }

        [Column("created_by")]
        public Guid CreatedBy { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("start_geo_id")]
        public int StartGeoId { get; set; }

        [Column("end_geo_id")]
        public int EndGeoId { get; set; }

        [Column("week_days")]
        public string WeekDays { get; set; }

        [Column("driver_name")]
        public string DriverName { get; set; }

        [Column("vehicle_no")]
        public string VehicleNo { get; set; }

        [Column("driver_phone")]
        public string DriverPhone { get; set; }

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
