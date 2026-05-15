using System.Data;
using System.Globalization;
using System.Text.Json;
using Dapper;
using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Infrastructure.ConnectionFactory;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;
using Infrastructure.ExternalServices;

namespace FleetBharat.TMSService.Infrastructure.Repository.Implementation
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly CommonApiClient _commonApi;

        public DashboardRepository(IDbConnectionFactory connectionFactory, CommonApiClient commonApi)
        {
            _connectionFactory = connectionFactory;
            _commonApi = commonApi;
        }

        public async Task<TripDashboardDto> GetTripDashboardAsync(int accountId)
        {
            var sql = """
            WITH detail_agg AS (
                SELECT
                    dt.trip_id,
                    COUNT(*)::integer AS segmentCount,
                    COALESCE(SUM(
                        CASE
                            WHEN COALESCE(dt.distance, '') ~ '^[0-9]+(\.[0-9]+)?$' THEN dt.distance::numeric
                            ELSE 0
                        END
                    ), 0)::numeric AS totalDistance
                FROM "TMS"."Det_Trip" dt
                GROUP BY dt.trip_id
            ),
            segment_progress AS (
                SELECT
                    dt.trip_id,
                    COALESCE(SUM(
                        CASE
                            WHEN COALESCE(dt.distance, '') ~ '^[0-9]+(\.[0-9]+)?$' THEN dt.distance::numeric
                            ELSE 0
                        END
                        * CASE
                            WHEN NOW() >= dt.segment_rta THEN 1
                            WHEN NOW() <= dt.segment_etd THEN 0
                            WHEN dt.segment_rta > dt.segment_etd THEN
                                LEAST(1, GREATEST(0, EXTRACT(EPOCH FROM NOW() - dt.segment_etd) / NULLIF(EXTRACT(EPOCH FROM dt.segment_rta - dt.segment_etd), 0)))
                            ELSE 0
                        END
                    ), 0)::numeric AS estimatedDistanceTravelled
                FROM "TMS"."Det_Trip" dt
                GROUP BY dt.trip_id
            ),
            enriched_trips AS (
                SELECT
                    t.trip_id AS tripId,
                    COALESCE(NULLIF(t.trip_no, ''), 'TRIP-' || t.trip_id::text) AS tripNo,
                    COALESCE(t.driver_name, '') AS driverName,
                    COALESCE(t.vehicle_no, '') AS vehicleNo,
                    CASE
                        WHEN COALESCE(t.trip_completed, false) = true
                            OR NULLIF(BTRIM(COALESCE(t.trip_status, '')), '') = 'TE' THEN 'Completed'
                        WHEN NULLIF(BTRIM(COALESCE(t.trip_status, '')), '') = 'TS'
                            AND COALESCE(t.legend_status, '') = 'DELAYED' THEN 'Delayed'
                        WHEN NULLIF(BTRIM(COALESCE(t.trip_status, '')), '') = 'TS' THEN 'In Transit'
                        WHEN NULLIF(BTRIM(COALESCE(t.trip_status, '')), '') IS NULL
                            AND NOW() > t.etd THEN 'Delayed'
                        WHEN COALESCE(t.is_current_trip, false) = true THEN 'Ready'
                        ELSE 'Planned'
                    END AS status,
                    COALESCE(NULLIF(BTRIM(COALESCE(t.trip_status, '')), ''), '') AS tripStatus,
                    t.etd AS etd,
                    t.rta AS rta,
                    COALESCE(t.start_geo_id, 0) AS startGeoId,
                    COALESCE(t.end_geo_id, 0) AS endGeoId,
                    COALESCE(t.is_active, false) AS isActive,
                    COALESCE(t.trip_completed, false) AS tripCompleted,
                    t.legend_status AS legendStatus,
                    CAST(t.geofence_json AS text) AS geofenceJson,
                    COALESCE(d.segmentCount, 0) AS segmentCount,
                    COALESCE(d.totalDistance, 0) AS totalDistance,
                    COALESCE(sp.estimatedDistanceTravelled, 0) AS estimatedDistanceTravelled
                FROM "TMS"."Trans_Trip" t
                LEFT JOIN detail_agg d ON d.trip_id = t.trip_id
                LEFT JOIN segment_progress sp ON sp.trip_id = t.trip_id
                WHERE t.account_id = @AccountId
                  AND COALESCE(t.is_active, false) = true
            ),
            status_filtered AS (
                SELECT *
                FROM enriched_trips
            )
            SELECT
                COUNT(*)::integer AS totalTrips,
                (COUNT(*) FILTER (WHERE COALESCE(isActive, false) = true))::integer AS activeTrips,
                (COUNT(*) FILTER (WHERE tripStatus = 'TS'))::integer AS inTransitTrips,
                (COUNT(*) FILTER (WHERE status = 'Delayed'))::integer AS delayedTrips,
                (COUNT(*) FILTER (WHERE tripStatus = 'TE'))::integer AS completedTrips
            FROM status_filtered;

            WITH detail_agg AS (
                SELECT
                    dt.trip_id,
                    COUNT(*)::integer AS segmentCount,
                    COALESCE(SUM(
                        CASE
                            WHEN COALESCE(dt.distance, '') ~ '^[0-9]+(\.[0-9]+)?$' THEN dt.distance::numeric
                            ELSE 0
                        END
                    ), 0)::numeric AS totalDistance
                FROM "TMS"."Det_Trip" dt
                GROUP BY dt.trip_id
            ),
            segment_progress AS (
                SELECT
                    dt.trip_id,
                    COALESCE(SUM(
                        CASE
                            WHEN COALESCE(dt.distance, '') ~ '^[0-9]+(\.[0-9]+)?$' THEN dt.distance::numeric
                            ELSE 0
                        END
                        * CASE
                            WHEN NOW() >= dt.segment_rta THEN 1
                            WHEN NOW() <= dt.segment_etd THEN 0
                            WHEN dt.segment_rta > dt.segment_etd THEN
                                LEAST(1, GREATEST(0, EXTRACT(EPOCH FROM NOW() - dt.segment_etd) / NULLIF(EXTRACT(EPOCH FROM dt.segment_rta - dt.segment_etd), 0)))
                            ELSE 0
                        END
                    ), 0)::numeric AS estimatedDistanceTravelled
                FROM "TMS"."Det_Trip" dt
                GROUP BY dt.trip_id
            ),
            enriched_trips AS (
                SELECT
                    t.trip_id AS tripId,
                    COALESCE(NULLIF(t.trip_no, ''), 'TRIP-' || t.trip_id::text) AS tripNo,
                    COALESCE(t.driver_name, '') AS driverName,
                    COALESCE(t.vehicle_no, '') AS vehicleNo,
                    CASE
                        WHEN COALESCE(t.trip_completed, false) = true
                            OR NULLIF(BTRIM(COALESCE(t.trip_status, '')), '') = 'TE' THEN 'Completed'
                        WHEN NULLIF(BTRIM(COALESCE(t.trip_status, '')), '') = 'TS'
                            AND COALESCE(t.legend_status, '') = 'DELAYED' THEN 'Delayed'
                        WHEN NULLIF(BTRIM(COALESCE(t.trip_status, '')), '') = 'TS' THEN 'In Transit'
                        WHEN NULLIF(BTRIM(COALESCE(t.trip_status, '')), '') IS NULL
                            AND NOW() > t.etd THEN 'Delayed'
                        WHEN COALESCE(t.is_current_trip, false) = true THEN 'Ready'
                        ELSE 'Planned'
                    END AS status,
                    COALESCE(NULLIF(BTRIM(COALESCE(t.trip_status, '')), ''), '') AS tripStatus,
                    t.etd AS etd,
                    t.rta AS rta,
                    t.atd AS atd,
                    t.ata AS ata,
                    t.start_in_datetime AS startindatetime,
                    t.start_out_datetime AS startoutdatetime,
                    t.end_in_datetime AS endindatetime,
                    t.end_out_datetime AS endoutdatetime,
                    COALESCE(t.start_geo_id, 0) AS startGeoId,
                    COALESCE(t.end_geo_id, 0) AS endGeoId,
                    COALESCE(t.trip_completed, false) AS tripCompleted,
                    t.legend_status AS legendStatus,
                    CAST(t.geofence_json AS text) AS geofenceJson,
                    COALESCE(d.segmentCount, 0) AS segmentCount,
                    COALESCE(d.totalDistance, 0) AS totalDistance,
                    COALESCE(sp.estimatedDistanceTravelled, 0) AS estimatedDistanceTravelled
                FROM "TMS"."Trans_Trip" t
                LEFT JOIN detail_agg d ON d.trip_id = t.trip_id
                LEFT JOIN segment_progress sp ON sp.trip_id = t.trip_id
                WHERE t.account_id = @AccountId
                  AND COALESCE(t.is_active, false) = true
            )
            SELECT
                tripId,
                tripNo,
                driverName,
                vehicleNo,
                status,
                etd,
                rta,
                atd,
                ata,
                startindatetime,
                startoutdatetime,
                endindatetime,
                endoutdatetime,
                startGeoId,
                endGeoId,
                tripCompleted,
                legendStatus,
                geofenceJson,
                segmentCount,
                totalDistance,
                estimatedDistanceTravelled
            FROM enriched_trips
            ORDER BY tripId DESC
            LIMIT 10;
            """;

            using var connection = _connectionFactory.CreateConnection();
            using var multi = await connection.QueryMultipleAsync(sql, new { AccountId = accountId });

            var summary = await multi.ReadFirstAsync<DashboardSummaryRow>();
            var rawTrips = (await multi.ReadAsync<DashboardTripRow>()).ToList();

            var geofenceLookup = await BuildGeofenceLookupAsync(accountId);

            var tripMonitor = rawTrips.Select(trip =>
            {
                var progress = ComputeProgress(trip.totalDistance, trip.estimatedDistanceTravelled, trip.etd, trip.rta, trip.tripCompleted, trip.status);
                return new TripMonitorItemDto
                {
                    tripCode = trip.tripNo,
                    vehicleCode = trip.vehicleNo,
                    driverName = trip.driverName,
                    from = ResolveGeofenceName(geofenceLookup, trip.startGeoId, trip.geofenceJson, "START"),
                    to = ResolveGeofenceName(geofenceLookup, trip.endGeoId, trip.geofenceJson, "END"),
                    lockStatus = GetLockStatus(trip),
                    lockState = GetLockState(trip),
                    progressPercent = progress,
                    progressColor = string.Equals(trip.status, "Delayed", StringComparison.OrdinalIgnoreCase) ? "red" : "purple",
                    statusLabel = $"{progress}%",
                    atd = FormatDateTime(trip.atd),
                    ata = FormatDateTime(trip.ata),
                    startInDatetime = FormatDateTime(trip.startindatetime),
                    startOutDatetime = FormatDateTime(trip.startoutdatetime),
                    endInDatetime = FormatDateTime(trip.endindatetime),
                    endOutDatetime = FormatDateTime(trip.endoutdatetime)
                };
            }).ToList();

            var dashboard = new TripDashboardDto
            {
                summaryCards = new List<TripDashboardMetricDto>
                {
                    new() { key = "totalTrips", label = "Total Trips", count = summary.totalTrips, trendText = "+0%", trendValue = 0, trendDirection = "neutral", status = "Overview" },
                    new() { key = "activeTrips", label = "Active Trips", count = summary.activeTrips, trendText = "+0%", trendValue = 0, trendDirection = "neutral", status = "Overview" },
                    new() { key = "inTransit", label = "In Transit", count = summary.inTransitTrips, trendText = "+0%", trendValue = 0, trendDirection = "neutral", status = "Live Monitoring" },
                    new() { key = "delayedTrips", label = "Delayed", count = summary.delayedTrips, trendText = "+0%", trendValue = 0, trendDirection = "neutral", status = "Live Monitoring" },
                    new() { key = "completed", label = "Completed", count = summary.completedTrips, trendText = "+0%", trendValue = 0, trendDirection = "neutral", status = "Live Monitoring" }
                },
                tripMonitor = tripMonitor,
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

            return dashboard;
        }

        private static string GetLockStatus(DashboardTripRow trip)
        {
            return string.Equals(trip.status, "Completed", StringComparison.OrdinalIgnoreCase)
                ? "UNLOCKED"
                : "LOCKED";
        }

        private static string GetLockState(DashboardTripRow trip)
        {
            return string.Equals(trip.status, "Completed", StringComparison.OrdinalIgnoreCase)
                ? "Unlocked"
                : "Locked";
        }

        private static int ComputeProgress(decimal totalDistance, decimal estimatedDistanceTravelled, DateTime? etd, DateTime? rta, bool tripCompleted, string? status)
        {
            if (tripCompleted || string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                return 100;
            }

            if (totalDistance <= 0)
            {
                return 0;
            }

            var ratio = totalDistance > 0 ? estimatedDistanceTravelled / totalDistance : 0;
            var percent = (int)Math.Clamp((double)(ratio * 100m), 0.0, 100.0);

            if (percent >= 100)
            {
                return 99;
            }

            if (percent == 0 && etd.HasValue && DateTime.UtcNow >= etd.Value && rta.HasValue && DateTime.UtcNow < rta.Value)
            {
                return 1;
            }

            return percent;
        }

        private static string FormatDateTime(DateTime? dateTime)
        {
            return dateTime.HasValue
                ? dateTime.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private async Task<Dictionary<int, string>> BuildGeofenceLookupAsync(int accountId)
        {
            try
            {
                var geofences = await _commonApi.GetGeofencesAsync(accountId, 500);
                return geofences
                    .Where(x => x != null)
                    .GroupBy(x => x.id)
                    .ToDictionary(x => x.Key, x => x.First().value ?? string.Empty);
            }
            catch
            {
                return new Dictionary<int, string>();
            }
        }

        private static string ResolveGeofenceName(Dictionary<int, string> geofenceLookup, int geoId, string? geofenceJson, string pointType)
        {
            if (geoId > 0 && geofenceLookup.TryGetValue(geoId, out var name) && !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            return TryResolveGeofenceNameFromJson(geofenceJson, geoId, pointType) ?? string.Empty;
        }

        private static string? TryResolveGeofenceNameFromJson(string? geofenceJson, int geoId, string pointType)
        {
            if (string.IsNullOrWhiteSpace(geofenceJson))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(geofenceJson);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                foreach (var point in document.RootElement.EnumerateArray())
                {
                    var currentGeoId = ReadInt(point, "geofenceId");
                    var currentPointType = ReadString(point, "pointType");

                    if ((currentGeoId.HasValue && currentGeoId.Value == geoId) ||
                        string.Equals(currentPointType, pointType, StringComparison.OrdinalIgnoreCase))
                    {
                        var address = ReadString(point, "geofenceAddress");
                        if (!string.IsNullOrWhiteSpace(address))
                        {
                            return address;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                return null;
            }

            return null;
        }

        private static int? ReadInt(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                return null;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var numericValue))
            {
                return numericValue;
            }

            if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out var stringValue))
            {
                return stringValue;
            }

            return null;
        }

        private static string? ReadString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                return null;
            }

            return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
        }

        private class DashboardSummaryRow
        {
            public int totalTrips { get; set; }
            public int activeTrips { get; set; }
            public int inTransitTrips { get; set; }
            public int delayedTrips { get; set; }
            public int completedTrips { get; set; }
        }

        private class DashboardTripRow
        {
            public string tripNo { get; set; } = string.Empty;
            public string driverName { get; set; } = string.Empty;
            public string vehicleNo { get; set; } = string.Empty;
            public string? status { get; set; }
            public DateTime? etd { get; set; }
            public DateTime? rta { get; set; }
            public DateTime? atd { get; set; }
            public DateTime? ata { get; set; }
            public DateTime? startindatetime { get; set; }
            public DateTime? startoutdatetime { get; set; }
            public DateTime? endindatetime { get; set; }
            public DateTime? endoutdatetime { get; set; }
            public int startGeoId { get; set; }
            public int endGeoId { get; set; }
            public bool tripCompleted { get; set; }
            public string? legendStatus { get; set; }
            public string? geofenceJson { get; set; }
            public int segmentCount { get; set; }
            public decimal totalDistance { get; set; }
            public decimal estimatedDistanceTravelled { get; set; }
        }
    }
}
