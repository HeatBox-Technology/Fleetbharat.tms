using Dapper;
using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Infrastructure.ConnectionFactory;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;

namespace FleetBharat.TMSService.Infrastructure.Repository.Implementation
{
    public class TripReportRepository : ITripReportRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public TripReportRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<(TripReportSummaryDto Summary, IEnumerable<TripReportDbRowDto> Items, int TotalRecords)> GetTripReportAsync(TripReportRepositoryRequestDto request)
        {
            var commonCte = """
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
            enriched_trips AS (
                SELECT
                    t.trip_id AS tripId,
                    COALESCE(NULLIF(t.trip_no, ''), 'TRIP-' || t.trip_id::text) AS tripNo,
                    t.account_id AS accountId,
                    t.driver_id AS driverId,
                    COALESCE(t.driver_name, '') AS driverName,
                    t.vehicle_id AS vehicleId,
                    COALESCE(t.vehicle_no, '') AS vehicleNo,
                    CASE
                        WHEN UPPER(COALESCE(t.trip_type, '')) IN ('ELOCK', 'E-LOCK', 'E_LOCK') THEN 'E-Lock'
                        WHEN UPPER(COALESCE(t.trip_type, '')) = 'LOGGER' THEN 'Logger'
                        WHEN UPPER(COALESCE(t.trip_type, '')) = 'GPS' THEN 'GPS'
                        WHEN COALESCE(t.is_elock, false) = true THEN 'E-Lock'
                        WHEN COALESCE(t.is_gps, false) = true THEN 'GPS'
                        WHEN COALESCE(t.primary_device, '') ILIKE '%logger%' THEN 'Logger'
                        WHEN COALESCE(t.primary_device, '') ILIKE '%lock%' THEN 'E-Lock'
                        WHEN COALESCE(t.primary_device, '') <> '' THEN 'GPS'
                        ELSE ''
                    END AS deviceType,
                    t.primary_device AS deviceNumber,
                    t.start_geo_id AS startGeoId,
                    t.end_geo_id AS endGeoId,
                    CASE
                        WHEN UPPER(COALESCE(t.trip_type, '')) = 'REVERSE' THEN 'Reverse'
                        ELSE 'Forward'
                    END AS tripDirection,
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
                    t.etd AS etd,
                    t.rta AS rta,
                    COALESCE(t.is_current_trip, false) AS isCurrentTrip,
                    COALESCE(t.trip_completed, false) AS tripCompleted,
                    t.legend_status AS legendStatus,
                    t.legend_icon AS legendIcon,
                    COALESCE(d.segmentCount, 0) AS segmentCount,
                    COALESCE(d.totalDistance, 0) AS totalDistance,
                    CAST(t.geofence_json AS text) AS geofenceJson
                FROM "TMS"."Trans_Trip" t
                LEFT JOIN detail_agg d ON d.trip_id = t.trip_id
                WHERE t.account_id = @AccountId
                  AND COALESCE(t.is_active, false) = true
                  AND (@VehicleNo IS NULL OR LOWER(COALESCE(t.vehicle_no, '')) = LOWER(@VehicleNo))
                  AND (@FromDate IS NULL OR t.etd >= @FromDate)
                  AND (@ToDate IS NULL OR t.etd <= @ToDate)
            ),
            base_filtered AS (
                SELECT *
                FROM enriched_trips
                WHERE (@DeviceType IS NULL OR deviceType = @DeviceType)
                  AND (@TripDirection IS NULL OR tripDirection = @TripDirection)
            ),
            status_filtered AS (
                SELECT *
                FROM base_filtered
                WHERE (@TripStatus IS NULL OR status = @TripStatus)
            )
            """;

            var sql = $"""
            {commonCte}
            SELECT
                COUNT(*)::integer AS totalTrips,
                (COUNT(*) FILTER (WHERE status = 'In Transit'))::integer AS inTransitTrips,
                (COUNT(*) FILTER (WHERE status = 'Completed'))::integer AS completedTrips,
                (COUNT(*) FILTER (WHERE status = 'Planned'))::integer AS plannedTrips,
                (COUNT(*) FILTER (WHERE status = 'Delayed'))::integer AS delayedTrips,
                (COUNT(*) FILTER (WHERE status = 'Ready'))::integer AS readyTrips
            FROM status_filtered;

            {commonCte}
            SELECT COUNT(*)::integer
            FROM status_filtered;

            {commonCte}
            SELECT
                tripId,
                tripNo,
                accountId,
                driverId,
                driverName,
                vehicleId,
                vehicleNo,
                deviceType,
                deviceNumber,
                startGeoId,
                endGeoId,
                tripDirection,
                status,
                etd,
                rta,
                isCurrentTrip,
                tripCompleted,
                legendStatus,
                legendIcon,
                segmentCount,
                totalDistance,
                geofenceJson
            FROM status_filtered
            ORDER BY tripId DESC;
            """;

            using var connection = _connectionFactory.CreateConnection();
            using var multi = await connection.QueryMultipleAsync(sql, new
            {
                AccountId = request.accountId,
                VehicleNo = request.vehicleNo,
                TripStatus = request.tripStatus,
                DeviceType = request.deviceType,
                TripDirection = request.tripType,
                FromDate = request.fromDate,
                ToDate = request.toDate
            });

            var summary = await multi.ReadFirstAsync<TripReportSummaryDto>();
            var totalRecords = await multi.ReadFirstAsync<int>();
            var items = await multi.ReadAsync<TripReportDbRowDto>();

            return (summary, items, totalRecords);
        }
    }
}
