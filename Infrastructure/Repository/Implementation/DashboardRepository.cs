using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;

namespace FleetBharat.TMSService.Infrastructure.Repository.Implementation
{
    public class DashboardRepository : IDashboardRepository
    {
        public Task<TripDashboardDto> GetTripDashboardAsync()
        {
            var dashboard = new TripDashboardDto
            {
                summaryCards = new List<TripDashboardMetricDto>
                {
                    new() { key = "activeTrips", label = "Active Trips", count = 124, trendText = "+12%", trendValue = 12, trendDirection = "positive", status = "Live Monitoring" },
                    new() { key = "inTransit", label = "In Transit", count = 86, trendText = "+8%", trendValue = 8, trendDirection = "positive", status = "Live Monitoring" },
                    new() { key = "lowBattery", label = "Low Battery", count = 3, trendText = "-2", trendValue = -2, trendDirection = "negative", status = "Live Monitoring" },
                    new() { key = "unauthorized", label = "Unauthorized", count = 0, trendText = "-1", trendValue = -1, trendDirection = "negative", status = "Live Monitoring" },
                    new() { key = "completed", label = "Completed", count = 412, trendText = "+45", trendValue = 45, trendDirection = "positive", status = "Live Monitoring" },
                    new() { key = "tamperEvents", label = "Tamper Events", count = 2, trendText = "0", trendValue = 0, trendDirection = "neutral", status = "Live Monitoring" }
                },
                tripMonitor = new List<TripMonitorItemDto>
                {
                    new()
                    {
                        tripCode = "SHP-2024-001",
                        vehicleCode = "TRK-582",
                        driverName = "John Doe",
                        from = "Lonavala",
                        to = "Pune",
                        lockStatus = "LOCKED",
                        lockState = "Locked",
                        progressPercent = 65,
                        progressColor = "purple",
                        statusLabel = "85%"
                    },
                    new()
                    {
                        tripCode = "SHP-2024-002",
                        vehicleCode = "VAN-441",
                        driverName = "Jane Smith",
                        from = "Okhla",
                        to = "Gurgaon",
                        lockStatus = "LOCKED",
                        lockState = "Locked",
                        progressPercent = 10,
                        progressColor = "purple",
                        statusLabel = "12%"
                    },
                    new()
                    {
                        tripCode = "SHP-2024-003",
                        vehicleCode = "TRK-901",
                        driverName = "Mike Ross",
                        from = "Mandya",
                        to = "Mysore",
                        lockStatus = "UNLOCKED",
                        lockState = "Unlocked",
                        progressPercent = 45,
                        progressColor = "red",
                        statusLabel = "64%"
                    },
                    new()
                    {
                        tripCode = "SHP-2024-004",
                        vehicleCode = "TRK-112",
                        driverName = "Sarah Connor",
                        from = "Kanchipuram",
                        to = "Vellore",
                        lockStatus = "LOCKED",
                        lockState = "Locked",
                        progressPercent = 85,
                        progressColor = "purple",
                        statusLabel = "92%"
                    }
                },
                tripTelemetry = new TripTelemetryDto
                {
                    temperature = "22°C",
                    humidity = "44%",
                    lastEvent = "Locked at Chennai Hub",
                    lastEventTime = "2 min ago"
                },
                securityFeed = new List<SecurityFeedItemDto>
                {
                    new() { severity = "CRITICAL", title = "T-103: Unauthorized Door Open Detected", location = "Warehouse A", label = "EVIDENCE", timestamp = "2m ago" },
                    new() { severity = "HIGH", title = "T-102: Low Battery - Security Risk", location = "HIGHWAY 44", label = "EVIDENCE", timestamp = "15m ago" },
                    new() { severity = "MEDIUM", title = "T-104: Route Deviation Alert", location = "DELHI OUTSKIRTS", label = "EVIDENCE", timestamp = "1h ago" }
                },
                aiAdvisor = new List<AIAdvisorItemDto>
                {
                    new() { title = "Predicted delay on T-104 due to congestion", subtitle = "AI PREDICTION", confidence = "88%", actionLabel = "ACCEPT" },
                    new() { title = "Route deviation detected on T-101", subtitle = "AI PREDICTION", confidence = "95%", actionLabel = "ACCEPT" }
                }
            };

            return Task.FromResult(dashboard);
        }
    }
}
