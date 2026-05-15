namespace FleetBharat.TMSService.Application.DTOs
{
    public class TripDashboardDto
    {
        public IEnumerable<TripDashboardMetricDto> summaryCards { get; set; } = new List<TripDashboardMetricDto>();
        public IEnumerable<TripMonitorItemDto> tripMonitor { get; set; } = new List<TripMonitorItemDto>();
        public TripTelemetryDto tripTelemetry { get; set; } = new TripTelemetryDto();
        public IEnumerable<SecurityFeedItemDto> securityFeed { get; set; } = new List<SecurityFeedItemDto>();
        public IEnumerable<AIAdvisorItemDto> aiAdvisor { get; set; } = new List<AIAdvisorItemDto>();
    }

    public class TripDashboardMetricDto
    {
        public string key { get; set; } = string.Empty;
        public string label { get; set; } = string.Empty;
        public int count { get; set; }
        public string trendText { get; set; } = string.Empty;
        public int trendValue { get; set; }
        public string trendDirection { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
    }

    public class TripMonitorItemDto
    {
        public string tripCode { get; set; } = string.Empty;
        public string vehicleCode { get; set; } = string.Empty;
        public string driverName { get; set; } = string.Empty;
        public string from { get; set; } = string.Empty;
        public string to { get; set; } = string.Empty;
        public string lockStatus { get; set; } = string.Empty;
        public string lockState { get; set; } = string.Empty;
        public int progressPercent { get; set; }
        public string progressColor { get; set; } = string.Empty;
        public string statusLabel { get; set; } = string.Empty;
        public string atd { get; set; } = string.Empty;
        public string ata { get; set; } = string.Empty;
        public string startInDatetime { get; set; } = string.Empty;
        public string startOutDatetime { get; set; } = string.Empty;
        public string endInDatetime { get; set; } = string.Empty;
        public string endOutDatetime { get; set; } = string.Empty;
    }

    public class TripTelemetryDto
    {
        public string temperature { get; set; } = string.Empty;
        public string humidity { get; set; } = string.Empty;
        public string lastEvent { get; set; } = string.Empty;
        public string lastEventTime { get; set; } = string.Empty;
    }

    public class SecurityFeedItemDto
    {
        public string severity { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public string location { get; set; } = string.Empty;
        public string label { get; set; } = string.Empty;
        public string timestamp { get; set; } = string.Empty;
    }

    public class AIAdvisorItemDto
    {
        public string title { get; set; } = string.Empty;
        public string subtitle { get; set; } = string.Empty;
        public string confidence { get; set; } = string.Empty;
        public string actionLabel { get; set; } = string.Empty;
    }
}
