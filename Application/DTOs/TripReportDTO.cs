namespace FleetBharat.TMSService.Application.DTOs
{
    public class TripReportFilterDto
    {
        public int accountId { get; set; }
        public string? vehicleNo { get; set; }
        public string? tripStatus { get; set; }
        public string? deviceType { get; set; }
        public string? tripType { get; set; }
        public string? fromDate { get; set; }
        public string? toDate { get; set; }
    }

    public class TripReportRepositoryRequestDto
    {
        public int accountId { get; set; }
        public string? vehicleNo { get; set; }
        public string? tripStatus { get; set; }
        public string? deviceType { get; set; }
        public string? tripType { get; set; }
        public DateTime? fromDate { get; set; }
        public DateTime? toDate { get; set; }
    }

    public class TripReportSummaryDto
    {
        public int totalTrips { get; set; }
        public int inTransitTrips { get; set; }
        public int completedTrips { get; set; }
        public int plannedTrips { get; set; }
        public int delayedTrips { get; set; }
        public int readyTrips { get; set; }
    }

    public class TripReportItemDto
    {
        public int tripId { get; set; }
        public string tripNo { get; set; } = string.Empty;
        public int accountId { get; set; }
        public string organization { get; set; } = string.Empty;
        public int driverId { get; set; }
        public string driverName { get; set; } = string.Empty;
        public int vehicleId { get; set; }
        public string vehicleNo { get; set; } = string.Empty;
        public string deviceType { get; set; } = string.Empty;
        public string? deviceNumber { get; set; }
        public string? lockStatus { get; set; }
        public int startGeoId { get; set; }
        public string origin { get; set; } = string.Empty;
        public int endGeoId { get; set; }
        public string destination { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public DateTime? etd { get; set; }
        public DateTime? rta { get; set; }
        public string startTime { get; set; } = string.Empty;
        public string eta { get; set; } = string.Empty;
        public bool isCurrentTrip { get; set; }
        public bool tripCompleted { get; set; }
        public string? legendStatus { get; set; }
        public string? legendIcon { get; set; }
        public int segmentCount { get; set; }
        public decimal totalDistance { get; set; }
    }

    public class TripReportListUiResponseDto
    {
        public TripReportSummaryDto summary { get; set; } = new();
        public int totalRecords { get; set; }
        public List<TripReportItemDto> data { get; set; } = new();
    }

    public class TripReportDbRowDto
    {
        public int tripId { get; set; }
        public string? tripNo { get; set; }
        public int accountId { get; set; }
        public int driverId { get; set; }
        public string? driverName { get; set; }
        public int vehicleId { get; set; }
        public string? vehicleNo { get; set; }
        public string? deviceType { get; set; }
        public string? deviceNumber { get; set; }
        public int startGeoId { get; set; }
        public int endGeoId { get; set; }
        public string? tripDirection { get; set; }
        public string? status { get; set; }
        public DateTime? etd { get; set; }
        public DateTime? rta { get; set; }
        public bool isCurrentTrip { get; set; }
        public bool tripCompleted { get; set; }
        public string? legendStatus { get; set; }
        public string? legendIcon { get; set; }
        public int segmentCount { get; set; }
        public decimal totalDistance { get; set; }
        public string? geofenceJson { get; set; }
    }
}
