namespace FleetBharat.TMSService.Application.DTOs
{
    public class TripPlanResponseDTO
    {
        public int planId { get; set; }
        public int accountId { get; set; }
        public string accountName { get; set; }
        public int driverId { get; set; }
        public string? driverName { get; set; }
        public int vehicleId { get; set; }
        public string? vehicleNo { get; set; }
        public string tripType { get; set; }
        public string travelDate { get; set; } // Formatted as dd/MM/yyyy
        public string plannedStartTime { get; set; }
        public int googleSuggestedTime { get; set; }
        public string plannedEndTime { get; set; }
        public int routeId { get; set; }
        public string routeName { get; set; } // Joined from Route table
        public int startGeoId { get; set; }
        public string startGeoName { get; set; }
        public int endGeoId { get; set; }
        public string endGeoName { get; set; }
        public DateTime createdDatetime { get; set; }
        public bool isActive { get; set; }
        public string frequency { get; set; }
    }

    public class TripPlanSummaryDto
    {
        public int TotalRecords { get; set; }
        public int TotalActive { get; set; }
        public int TotalInactive { get; set; }
    }

    public class TripPlanListUiResponseDto
    {
        public TripPlanSummaryDto Summary { get; set; } = new();

        public PagedResultDto<TripPlanResponseDTO> Data { get; set; } = new();
    }
}
