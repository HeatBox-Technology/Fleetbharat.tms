using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetBharat.TMSService.Domain.Entities.TMS
{
    [Table("Det_Trip", Schema = "TMS")]
    public class DetTrip
    {
        [Key]
        [Column("det_trip_id")]
        public int DetTripId { get; set; }

        [Column("trip_id")]
        public int TripId { get; set; }

        [Column("from_geo_id")]
        public int FromGeoId { get; set; }

        [Column("to_geo_id")]
        public int ToGeoId { get; set; }

        [Column("sequence_no")]
        public int SequenceNo { get; set; }

        [Column("distance")]
        public string Distance { get; set; }

        [Column("segment_etd", TypeName = "timestamp")]
        public DateTime SegmentETD { get; set; }

        [Column("segment_rta", TypeName = "timestamp")]
        public DateTime SegmentRTA { get; set; } // Renamed from SegmentETA

        [Column("lead_time_mins")]
        public int LeadTimeMins { get; set; }

        [Column("rta_mins")]
        public int RTAMins { get; set; }
    }
}
