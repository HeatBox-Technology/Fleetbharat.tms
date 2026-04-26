using Dapper;
using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Infrastructure.ConnectionFactory;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace FleetBharat.TMSService.Infrastructure.Repository.Implementation
{
    public class TripPlanRepository : ITripPlanRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public TripPlanRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> CreateTripPlanAsync(
            TripPlanRequestDTO request,
            DateTime? travelDate,
            int eta,
            string secondaryDevicesJson,
            string plannedEntryTime,
            string plannedExitTime,
            IDbTransaction transaction)
        {
            

            string sql = """
            INSERT INTO "TMS"."Trip_Plan"
            (
                account_id,
                driver_id,
                vehicle_id,
                frequency,
                travel_date,
                route_id,
                created_datetime,
                start_geo_id,
                end_geo_id,
                created_by,
                week_days,
                driver_name,
                vehicle_no,
                driver_phone,
                primary_device,
                consignee,
                consignor,
                secondary_devices,
                vehicle_category,
                routing_model,
                route_path,
                google_suggested_time,
                planned_start_time,
                planned_end_time,
                trip_type
            )
            VALUES
            (
                @AccountId,
                @DriverId,
                @VehicleId,
                @Frequency,
                @TravelDate,
                @RouteId,
                @CreatedDatetime,
                @StartGeoId,
                @EndGeoId,
                @CreatedBy,
                @WeekDays,
                @DriverName,
                @VehicleNumber,
                @DriverPhone,
                @PrimaryDevice,
                @Consignee,
                @Consignor,
                @SecondaryDevice::jsonb,
                @VehicleCategory,
                @RoutingModel,
                @RoutePath,
                @Eta,
                @PlannedEntryTime,
                @PlannedExitTime,
                @TripType
            )
            RETURNING plan_id;
        """;

            int planId = await transaction.Connection!.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    request.accountId,
                    request.driverId,
                    request.vehicleId,
                    Frequency=request.frequency,
                    TravelDate = travelDate,
                    request.routeId,
                    CreatedDatetime = DateTime.UtcNow,
                    request.startGeoId,
                    request.endGeoId,
                    request.createdBy,
                    request.weekDays,
                    request.driverName,
                    request.vehicleNumber,
                    request.driverPhone,
                    request.primaryDevice,
                    request.Consignee,
                    request.Consignor,
                    SecondaryDevice=secondaryDevicesJson,
                    request.vehicleCategory,
                    request.routingModel,
                    request.routePath,
                    eta,
                    plannedEntryTime,
                    plannedExitTime,
                    request.tripType
                },
                transaction);

            return planId;
        }

        public async Task InsertRouteDetailsAsync(
        int planId,
        IEnumerable<TripPlanRouteDetailsDTO> routeDetails,
        IDbTransaction transaction)
        {
            string sql = """
            INSERT INTO "TMS"."Trip_Plan_Route_Detail"
            (
                plan_id,
                from_geo_id,
                to_geo_id,
                sequence,
                distance,
                lead_time,
                "RTA",
                google_suggested_time,
                from_exit_time,
                to_entry_time
            )
            VALUES
            (
                @PlanId,
                @FromGeoId,
                @ToGeoId,
                @Sequence,
                @Distance,
                @LeadTime,
                @RTA,
                @GoogleSuggestedTime,
                @FromExitTime,
                @ToEntryTime
            );
        """;

            var routeParams = routeDetails.Select(r => new
            {
                PlanId = planId,
                FromGeoId = r.fromGeoId,
                ToGeoId = r.toGeoId,
                Sequence = r.sequence,
                Distance = r.distance,
                LeadTime = r.leadTime,
                RTA = r.rta,
                GoogleSuggestedTime=r.googleSuggestedTime,
                FromExitTime = r.fromExitTime, // next stop entry
                ToEntryTime = r.toEntryTime    // current stop exit
            });

            await transaction.Connection!.ExecuteAsync(sql, routeParams, transaction);
        }


        public async Task InsertGeofencePointsAsync(
        int planId,
        List<TripPlanGeofenceRouteDetailsDTO> geofenceDetails,
        IDbTransaction transaction)
        {
            string sql = """
            INSERT INTO "TMS"."Trip_Plan_Geofence_Details"
            (
                plan_id,
                geofence_id,
                geofence_type,
                point_type,
                geofence_address,
                geo_center_latitude,
                geo_center_longitude,
                geo_radius,
                sequence,
                planned_entry_time,
                planned_exit_time,
                google_suggested_time,
                distance,
                geofence_coordinates
            )
            VALUES
            (
                @PlanId,
                @GeoId,
                @GeoType,
                @PoinType,
                @GeoAddress,
                @GeoCenterLat,
                @GeoCenterLong,
                @GeoRadius,
                @Sequence,
                @PlannedEntryTime,
                @PlannedExitTime,
                @GoogleSuggestedTime,
                @Distance,
                @geoJson::jsonb
            );
            """;

            var ordered = geofenceDetails
                .OrderBy(x => x.sequence)
                .ToList();

            var geofenceParams = ordered.Select(x =>
            {
                return new
                {
                    PlanId = planId,
                    GeoId = x.geofenceId,
                    GeoType=x.geofenceType,
                    PoinType=x.pointType,
                    GeoAddress=x.geofenceAddress,
                    GeoCenterLat=x.geofenceCenterLatitude,
                    GeoCenterLong=x.geofenceCenterLongitude,
                    GeoRadius=x.geofenceRadius,
                    Sequence = x.sequence,
                    PlannedEntryTime = x.plannedEntryTime,
                    PlannedExitTime = x.plannedExitTime,
                    GoogleSuggestedTime = x.googleSuggestedTime,
                    Distance=x.distance,
                    geoJson = x.geofenceDetails?.Any() == true
                    ? JsonSerializer.Serialize(x.geofenceDetails)
                    : "[]"

            };
            });

            await transaction.Connection!.ExecuteAsync(
                sql,
                geofenceParams,
                transaction);
        }


        public async Task CreateTransAndDetTripAsync(
        int planId,
        TripPlanRequestDTO request,
        List<TripPlanRouteDetailsDTO> segments,
        DateTime TripETD,
        DateTime TripRTA,
        string secondaryDevicesJson,
        IDbTransaction transaction)
        {
            // 1. Insert into Trans_Trip (Master)
            string transSql = """
            INSERT INTO "TMS"."Trans_Trip"
            (
            account_id, driver_id, vehicle_id, frequency, 
            travel_date, etd, rta, total_lead_time, 
            route_id, created_datetime, is_active,
            driver_name, vehicle_no, driver_phone,
            start_geo_id, end_geo_id, created_by,
            trip_type,primary_device, consignee, consignor,
            secondary_devices, vehicle_category, routing_model, route_path
            )
            VALUES
            (
            @AccountId, @DriverId, @VehicleId, @Frequency, 
            @TravelDate, @ETD, @RTA, @TotalLeadTime, 
            @RouteId, @CreatedDatetime, true,
            @driverName, @vehicleNumber, @driverPhone,
            @StartGeoId, @EndGeoId, @CreatedBy,
            @TripType, @PrimaryDevice, @Consignee, @Consignor,
            @SecondaryDevice::jsonb, 
            @VehicleCategory, @RoutingModel, @RoutePath
            )
            RETURNING trip_id;
            """;

            // We calculate the final RTA after processing details, but for the insert 
            // we'll update it or calculate it upfront. Let's calculate the timeline.
            DateTime currentTimeline = DateTime.SpecifyKind(TripETD, DateTimeKind.Utc);
            int totalRtaMins = segments.Sum(x => x.rta);
            int totalLeadTime = segments.Sum(x => x.leadTime);
            DateTime finalRta = TripRTA;

            int transTripId = await transaction.Connection.ExecuteScalarAsync<int>(transSql, new
            {
                request.accountId,
                request.driverId,
                request.vehicleId,
                Frequency=request.frequency,
                TravelDate = currentTimeline.Date,
                ETD = currentTimeline,
                RTA = finalRta,
                TotalLeadTime = totalLeadTime,
                request.routeId,
                CreatedDatetime = DateTime.UtcNow,
                request.driverName,
                request.vehicleNumber,
                request.driverPhone,
                request.startGeoId,
                request.endGeoId,
                request.createdBy,
                request.tripType,
                request.primaryDevice,
                request.Consignee,
                request.Consignor,
                SecondaryDevice=secondaryDevicesJson,
                request.vehicleCategory,
                request.routingModel,
                request.routePath
            }, transaction);

            // 2. Insert into Det_Trip (Details)
            string detSql = """
            INSERT INTO "TMS"."Det_Trip"
            (
            trip_id, from_geo_id, to_geo_id, sequence_no, 
            distance, segment_etd, segment_rta, lead_time_mins, rta_mins,
            google_suggested_time
            )
            VALUES
            (
            @TripId, @FromGeoId, @ToGeoId, @SequenceNo, 
            @Distance, @SegmentETD, @SegmentRTA, @LeadTimeMins, @RTAMins,
            @GoogleSuggestedTime
            );
            """;

            var detailList = segments.Select(x => new
            {
                TripId= transTripId,
                FromGeoId = x.fromGeoId,
                ToGeoId = x.toGeoId,
                SequenceNo = x.sequence,
                Distance = x.distance,
                SegmentETD = ParseDateTime(x.fromExitTime),
                SegmentRTA = ParseDateTime(x.toEntryTime),
                LeadTimeMins = x.leadTime,
                RTAMins = x.rta,
                GoogleSuggestedTime = x.googleSuggestedTime,
            }).ToList();

            

            await transaction.Connection.ExecuteAsync(detSql, detailList, transaction);
        }

        private DateTime ParseDateTime(string dateTime)
        {
            string[] formats =
            {
                "dd/MM/yyyy HH:mm",
                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yyyy hh:mm tt",
                "dd/MM/yyyy hh:mm:ss tt",
                "yyyy-MM-dd HH:mm:ss"
            };

            return DateTime.ParseExact(
            dateTime,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None);
            }

        public async Task<(IEnumerable<dynamic> Items, int Total, int TotalActive)> GetAllTripPlansAsync(int accountId, int page, int pageSize)
        {


            // Offset calculation
            int offset = (page - 1) * pageSize;

            // 1. Total records for this account
            // 2. Total active records for this account
            // 3. Paged list with Route Name join
            string sql = """
            SELECT COUNT(*) FROM "TMS"."Trip_Plan" WHERE account_id = @AccountId;
        
            SELECT COUNT(*) FROM "TMS"."Trip_Plan" WHERE account_id = @AccountId AND is_active = true;

            SELECT 
            tp.*, 
            r.route_name 
            FROM "TMS"."Trip_Plan" tp
            LEFT JOIN "TMS"."mst_route" r ON tp.route_id = r.route_id
            WHERE tp.account_id = @AccountId
            ORDER BY tp.plan_id DESC
            OFFSET @Offset LIMIT @Limit;
            """;
            using var connetion = _connectionFactory.CreateConnection();
            {
                using var multi = await connetion.QueryMultipleAsync(sql, new
                {
                    AccountId = accountId,
                    Offset = offset,
                    Limit = pageSize
                });

                int total = await multi.ReadFirstAsync<int>();
                int totalActive = await multi.ReadFirstAsync<int>();
                var items = await multi.ReadAsync<dynamic>(); // Using dynamic to capture joined route_name

                return (items, total, totalActive);
            }
        }


        public async Task<bool> DeleteTripPlanAsync(int planId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                // Delete child route details first to satisfy foreign key constraints
                var deleteDetails = @"DELETE FROM ""TMS"".""Trip_Plan_Route_Detail"" WHERE plan_id = @Id";
                await connection.ExecuteAsync(deleteDetails, new { Id = planId }, transaction);

                // Delete the main trip plan
                var deletePlan = @"DELETE FROM ""TMS"".""Trip_Plan"" WHERE plan_id = @Id";
                int affected = await connection.ExecuteAsync(deletePlan, new { Id = planId }, transaction);

                transaction.Commit();
                return affected > 0;
            }
            catch
            {
                transaction.Rollback();
                throw; // Re-throw to be caught by the service or middleware
            }
        }

        public async Task<bool> UpdateTripPlanAsync(
        int planId,
        TripPlanRequestDTO request,
        DateTime? travelDate,
        int totalETA,
        string secondaryDevicesJson,
        string plannedEntryTime,
        string plannedExitTime,
        IDbTransaction transaction)
        {
            string sql = """
            UPDATE "TMS"."Trip_Plan"
            SET 
                account_id = @AccountId,
                driver_id = @DriverId,
                vehicle_id = @VehicleId,
                frequency = @Frequency,
                travel_date = @TravelDate,
                route_id = @RouteId,
                start_geo_id = @StartGeoId,
                end_geo_id = @EndGeoId,
                week_days = @WeekDays,
                driver_name = @DriverName,
                vehicle_no = @VehicleNumber,
                driver_phone = @DriverPhone,
                trip_type = @TripType,
                primary_device = @PrimaryDevice,
                consignee = @Consignee,
                consignor = @Consignor,
                secondary_devices=@SecondaryDevicesJson::jsonb,
                vehicle_category=@VehicleCategory,
                routing_model=@RoutingModel,
                route_path=@RoutePath,
                google_suggested_time=@GoogleSuggestedTime,
                planned_start_time=@PlannedEntryTime,
                planned_end_time=@PlannedExitTime

            WHERE plan_id = @PlanId;
            """;

            int rowsAffected = await transaction.Connection!.ExecuteAsync(sql, new
            {
                PlanId = planId,
                AccountId = request.accountId,
                DriverId = request.driverId,
                VehicleId = request.vehicleId,
                Frequency = request.frequency,
                TravelDate = travelDate,
                request.routeId,
                request.startGeoId,
                request.endGeoId,
                request.weekDays,
                request.driverName,
                request.vehicleNumber,
                request.driverPhone,
                TripType = request.tripType,
                request.primaryDevice,
                request.Consignee,
                request.Consignor,
                secondaryDevicesJson,
                request.vehicleCategory,
                request.routingModel,
                request.routePath,
                googleSuggestedTime= totalETA,
                plannedEntryTime,
                plannedExitTime
            }, transaction);

            return rowsAffected > 0;
        }

        public async Task DeleteRouteDetailsByPlanIdAsync(int planId, IDbTransaction transaction)
        {
            string sql = @"DELETE FROM ""TMS"".""Trip_Plan_Route_Detail"" WHERE plan_id = @PlanId";
            await transaction.Connection!.ExecuteAsync(sql, new { PlanId = planId }, transaction);
        }

        public async Task DeleteGeofenceDetailsByPlanIdAsync(int planId, IDbTransaction transaction)
        {
            string sql = @"DELETE FROM ""TMS"".""Trip_Plan_Geofence_Details"" WHERE plan_id = @PlanId";
            await transaction.Connection!.ExecuteAsync(sql, new { PlanId = planId }, transaction);
        }


        public async Task<TripPlanByIdResponseDTO?> GetTripPlanByIdAsync(int planId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            string sql = """
            SELECT 
                plan_id AS planId, account_id AS accountId, driver_id AS driverId,
                vehicle_id AS vehicleId, frequency AS frequency, travel_date AS travel_date,
                route_id AS routeId,
                start_geo_id AS startGeoId, end_geo_id AS endGeoId, week_days AS weekDays,
                created_datetime AS createdDatetime, created_by AS createdBy, driver_name AS driverName,
                vehicle_no AS vehicleNo, driver_phone AS driverPhone,routing_model AS routingModel,
                CASE 
                WHEN vehicle_id = 0 THEN 'EXTERNAL'
                ELSE 'INTERNAL'
                END AS fleetSource,
                route_path AS routePath,
                is_elock AS isElockTrip,
                is_gps AS isGPSTrip,
                primary_device AS primaryDevice,
                consignee AS consignee,
                consignor AS consignor,
                secondary_devices AS secondaryDevicesJson,
                vehicle_category AS vehicleCategory,
                trip_type AS tripType
            FROM "TMS"."Trip_Plan"
            WHERE plan_id = @Id
            """;

            return await connection.QueryFirstOrDefaultAsync<TripPlanByIdResponseDTO>(sql, new { Id = planId });
        }

        // Method 2: Get Child Route Details
        public async Task<IEnumerable<TripPlanRouteDetailsDTO>> GetRouteDetailsByPlanIdAsync(int planId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            string sql = """
            SELECT 
                plan_id AS planId, from_geo_id AS fromGeoId, to_geo_id AS toGeoId,
                sequence, distance, lead_time AS leadTime, "RTA" AS rta
            FROM "TMS"."Trip_Plan_Route_Detail"
            WHERE plan_id = @Id
            ORDER BY sequence ASC
            """;

            return await connection.QueryAsync<TripPlanRouteDetailsDTO>(sql, new { Id = planId });
        }

        public async Task<IEnumerable<TripPlanGeofenceDbResponseDTO>> GetGeofenceDetailsByPlanIdAsync(int planId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            string sql = """
            SELECT 
                plan_id AS planId, 
                geofence_id AS geofenceId, 
                geofence_type AS geofenceType,
                point_type AS pointType,
                geofence_address AS geofenceAddress,
                geo_center_latitude AS geofenceCenterLatitude,
                geo_center_longitude AS geofenceCenterLongitude,
                geo_radius AS geofenceRadius,
                planned_entry_time AS plannedEntryTime,
                planned_exit_time AS plannedExitTime,
                sequence, 
                distance, 
                google_suggested_time AS googleSuggestedTime,
                geofence_coordinates AS geofenceDetails
            FROM "TMS"."Trip_Plan_Geofence_Details"
            WHERE plan_id = @Id
            ORDER BY sequence ASC
            """;

            return await connection.QueryAsync<TripPlanGeofenceDbResponseDTO>(sql, new { Id = planId });
        }


        public async Task<List<TripDbDTO>> GetTripsForOverlapCheck(
        string vehicleNo,
        int? planId,
        IDbTransaction transaction)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            var query = @"
            SELECT plan_id AS TripPlanId, 
            trip_type AS Frequency, 
            planned_end_time AS PlannedEndTime, 
            planned_start_time AS PlannedStartTime, 
            week_days AS WeekDays,
            travel_date AS TravelDate,
            google_suggested_time AS TotalETA
            FROM ""TMS"".""Trip_Plan""
            WHERE vehicle_no = @VehicleNo
            AND plan_id <> COALESCE(@PlanId, 0);
            ";

            return (await connection.QueryAsync<TripDbDTO>(
                query,
                new { VehicleNo = vehicleNo, PlanId = planId },
                transaction
            )).ToList();
        }
    }
}
