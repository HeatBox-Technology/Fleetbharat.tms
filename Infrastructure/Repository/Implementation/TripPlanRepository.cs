using Dapper;
using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Infrastructure.ConnectionFactory;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Data;
using System.Data.Common;
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
            int leadTime,
            int eta,
            IDbTransaction transaction)
        {
            string sql = """
            INSERT INTO "TMS"."Trip_Plan"
            (
                account_id,
                driver_id,
                vehicle_id,
                trip_type,
                travel_date,
                "ETD",
                lead_time,
                "ETA",
                route_id,
                created_datetime,
                start_geo_id,
                end_geo_id,
                created_by,
                week_days,
                driver_name,
                vehicle_no,
                driver_phone,
                is_elock,
                is_gps,
                primary_device,
                consignee,
                consignor,
                secondary_device,
                vehicle_category,
                routing_model,
                route_path
            )
            VALUES
            (
                @AccountId,
                @DriverId,
                @VehicleId,
                @TripType,
                @TravelDate,
                @ETD,
                @LeadTime,
                @ETA,
                @RouteId,
                @CreatedDatetime,
                @StartGeoId,
                @EndGeoId,
                @CreatedBy,
                @WeekDays,
                @DriverName,
                @VehicleNumber,
                @DriverPhone,
                @IsElockTrip,
                @IsGPSTrip,
                @PrimaryDevice,
                @Consignee,
                @Consignor,
                @SecondaryDevice,
                @VehicleCategory,
                @RoutingModel,
                @RoutePath
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
                    TripType=request.frequency,
                    TravelDate = travelDate,
                    ETD = request.etd,
                    LeadTime = leadTime,
                    ETA = eta,
                    request.routeId,
                    CreatedDatetime = DateTime.UtcNow,
                    request.startGeoId,
                    request.endGeoId,
                    request.createdBy,
                    request.weekDays,
                    request.driverName,
                    request.vehicleNumber,
                    request.driverPhone,
                    request.isElockTrip,
                    request.isGPSTrip,
                    request.primaryDevice,
                    request.Consignee,
                    request.Consignor,
                    request.secondaryDevice,
                    request.vehicleCategory,
                    request.routingModel,
                    request.routePath
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
                "RTA"
            )
            VALUES
            (
                @PlanId,
                @FromGeoId,
                @ToGeoId,
                @Sequence,
                @Distance,
                @LeadTime,
                @RTA
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
                RTA = r.rta
            });

            await transaction.Connection!.ExecuteAsync(sql, routeParams, transaction);
        }


        public async Task CreateTransAndDetTripAsync(
        int planId,
        TripPlanRequestDTO request,
        DateTime baseTimeline,
        IDbTransaction transaction)
        {
            // 1. Insert into Trans_Trip (Master)
            string transSql = """
            INSERT INTO "TMS"."Trans_Trip"
            (
            account_id, driver_id, vehicle_id, trip_type, 
            travel_date, etd, rta, total_lead_time, 
            route_id, created_datetime, is_active,
            driver_name, vehicle_no, driver_phone,
            start_geo_id, end_geo_id, created_by,
            is_elock, is_gps, primary_device, consignee, consignor,
            secondary_device, vehicle_category, routing_model, route_path
            )
            VALUES
            (
            @AccountId, @DriverId, @VehicleId, @TripType, 
            @TravelDate, @ETD, @RTA, @TotalLeadTime, 
            @RouteId, @CreatedDatetime, true,
            @driverName, @vehicleNumber, @driverPhone,
            @StartGeoId, @EndGeoId, @CreatedBy,
            @IsElockTrip, @IsGPSTrip, @PrimaryDevice, @Consignee, @Consignor,
            @SecondaryDevice, @VehicleCategory, @RoutingModel, @RoutePath
            )
            RETURNING trip_id;
            """;

            // We calculate the final RTA after processing details, but for the insert 
            // we'll update it or calculate it upfront. Let's calculate the timeline.
            DateTime currentTimeline = DateTime.SpecifyKind(baseTimeline, DateTimeKind.Utc);
            int totalLeadTime = request.routeDetails.Sum(x => x.leadTime);
            int totalRtaMins = request.routeDetails.Sum(x => x.rta);
            DateTime finalRta = currentTimeline.AddMinutes(totalRtaMins + totalLeadTime);

            int transTripId = await transaction.Connection.ExecuteScalarAsync<int>(transSql, new
            {
                request.accountId,
                request.driverId,
                request.vehicleId,
                TripType=request.frequency,
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
                request.isElockTrip,
                request.isGPSTrip,
                request.primaryDevice,
                request.Consignee,
                request.Consignor,
                request.secondaryDevice,
                request.vehicleCategory,
                request.routingModel,
                request.routePath
            }, transaction);

            // 2. Insert into Det_Trip (Details)
            string detSql = """
            INSERT INTO "TMS"."Det_Trip"
            (
            trip_id, from_geo_id, to_geo_id, sequence_no, 
            distance, segment_etd, segment_rta, lead_time_mins, rta_mins
            )
            VALUES
            (
            @TripId, @FromGeoId, @ToGeoId, @SequenceNo, 
            @Distance, @SegmentETD, @SegmentRTA, @LeadTimeMins, @RTAMins
            );
            """;

            var segments = new List<object>();
            var sortedDetails = request.routeDetails.OrderBy(x => x.sequence).ToList();

            for (int i = 0; i < sortedDetails.Count; i++)
            {
                var item = sortedDetails[i];

                // Logic: ETD is current timeline. RTA is ETD + rta_mins.
                DateTime segmentEtd = currentTimeline;
                DateTime segmentRta = segmentEtd.AddMinutes(item.rta);

                segments.Add(new
                {
                    TripId = transTripId,
                    FromGeoId = item.fromGeoId,
                    ToGeoId = item.toGeoId,
                    SequenceNo = item.sequence,
                    Distance = item.distance,
                    SegmentETD = segmentEtd,
                    SegmentRTA = segmentRta,
                    LeadTimeMins = item.leadTime,
                    RTAMins = item.rta
                });

                // Advance timeline for NEXT segment: Current RTA + Current LeadTime
                currentTimeline = segmentRta.AddMinutes(item.leadTime);
            }

            await transaction.Connection.ExecuteAsync(detSql, segments, transaction);
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
        int leadTime,
        int eta,
        IDbTransaction transaction)
        {
            string sql = """
            UPDATE "TMS"."Trip_Plan"
            SET 
                account_id = @AccountId,
                driver_id = @DriverId,
                vehicle_id = @VehicleId,
                trip_type = @TripType,
                travel_date = @TravelDate,
                "ETD" = @ETD,
                lead_time = @LeadTime,
                "ETA" = @ETA,
                route_id = @RouteId,
                start_geo_id = @StartGeoId,
                end_geo_id = @EndGeoId,
                week_days = @WeekDays,
                driver_name = @DriverName,
                vehicle_no = @VehicleNumber,
                driver_phone = @DriverPhone,
                is_elock = @IsElockTrip,
                is_gps = @IsGPSTrip,
                primary_device = @PrimaryDevice,
                consignee = @Consignee,
                consignor = @Consignor,
                secondary_device=@SecondaryDevice,
                vehicle_category=@VehicleCategory,
                routing_model=@RoutingModel,
                route_path=@RoutePath

            WHERE plan_id = @PlanId;
            """;

            int rowsAffected = await transaction.Connection!.ExecuteAsync(sql, new
            {
                PlanId = planId,
                AccountId = request.accountId,
                DriverId = request.driverId,
                VehicleId = request.vehicleId,
                TripType = request.frequency,
                TravelDate = travelDate,
                ETD = request.etd,
                LeadTime = leadTime,
                ETA = eta,
                request.routeId,
                request.startGeoId,
                request.endGeoId,
                request.weekDays,
                request.driverName,
                request.vehicleNumber,
                request.driverPhone,
                request.isElockTrip,
                request.isGPSTrip,
                request.primaryDevice,
                request.Consignee,
                request.Consignor,
                request.secondaryDevice,
                request.vehicleCategory,
                request.routingModel,
                request.routePath
            }, transaction);

            return rowsAffected > 0;
        }

        public async Task DeleteRouteDetailsByPlanIdAsync(int planId, IDbTransaction transaction)
        {
            string sql = @"DELETE FROM ""TMS"".""Trip_Plan_Route_Detail"" WHERE plan_id = @PlanId";
            await transaction.Connection!.ExecuteAsync(sql, new { PlanId = planId }, transaction);
        }

        public async Task<TripPlanByIdResponseDTO?> GetTripPlanByIdAsync(int planId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            string sql = """
            SELECT 
                plan_id AS planId, account_id AS accountId, driver_id AS driverId,
                vehicle_id AS vehicleId, trip_type AS frequeny, travel_date AS travelDate,
                "ETD" AS etd, lead_time AS leadTime, "ETA" AS eta, route_id AS routeId,
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
                secondary_device AS secondayDevice,
                vehicle_category AS vehicleCategory
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
    }
}
