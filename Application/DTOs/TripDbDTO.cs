namespace FleetBharat.TMSService.Application.DTOs
{
    public class TripDbDTO
    {
        public int TripPlanId { get; set; }

        public string Frequency { get; set; }

        public DateTime? TravelDate { get; set; }

        public string PlannedStartTime { get; set; }  // string from DB

        public string PlannedEndTime { get; set; }    // string from DB

        public int TotalETA { get; set; }

        public string WeekDays { get; set; }
    }
}
