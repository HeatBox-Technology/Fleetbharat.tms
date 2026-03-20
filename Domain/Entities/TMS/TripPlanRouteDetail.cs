using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetBharat.TMSService.Domain.Entities.TMS
{
    [Table("Trip_Plan_Route_Detail", Schema = "TMS")]
    public class TripPlanRouteDetail
    {
        [Key]
        [Column("detail_plan_id")]
        public int DetailPlanId { get; set; }

        [Column("plan_id")]
        public int PlanId { get; set; }

        [Column("from_geo_id")]
        public int FromGeoId { get; set; }

        [Column("to_geo_id")]
        public int ToGeoId { get; set; }

        [Column("sequence")]
        public int Sequence { get; set; }

        [Column("distance")]
        public string Distance { get; set; }

        [Column("lead_time")]
        public int LeadTime { get; set; }

        [Column("RTA")]
        public int RTA { get; set; }
    }
}
