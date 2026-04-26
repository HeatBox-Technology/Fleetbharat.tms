using Dapper;
using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;
using System.Data;
using System.Globalization;

namespace FleetBharat.TMSService.Infrastructure.Repository.Implementation
{
    public class CreateTripRepository : ICreateTripRepository
    {
        public async Task<List<CreateTripDTO>> GetRecurringTrips(IDbTransaction transaction)
        {
            var query = @"
            SELECT 
            plan_id AS planId, 
            account_id AS accountId, 
            driver_id AS driverId,
            vehicle_id AS vehicleId, 
            trip_type AS frequency, 
            route_id AS routeId,
            start_geo_id AS startGeoId, 
            end_geo_id AS endGeoId, 
            week_days AS weekDays,
            driver_name AS driverName,
            vehicle_no AS vehicleNumber,
            driver_phone AS driverPhone,
            primary_device AS primaryDevice,
            planned_start_time AS plannedStartTime,
            planned_end_time AS plannedEndTime,
            google_suggested_time AS googleSuggestedTime,
            secondary_devices AS secondaryDevices,
            vehicle_category AS vehicleCategory,
            route_path AS routePath,
            routing_model AS routingModel,
            trip_type AS tripType,
            consignee AS Consignee,
            consignor AS Consignor,
            created_by AS createdBy

            FROM ""TMS"".""Trip_Plan""
            WHERE ""trip_type"" = 'RECURRING';
        ";

            var result = await transaction.Connection.QueryAsync<CreateTripDTO>(
                query,
                transaction: transaction
                );

            return result.ToList();
        }

        public async Task<bool> TripExistsAsync(
        IDbTransaction transaction,
        string vehicleNumber,
        DateTime plannedStartTime,
        DateTime plannedEndTime) 
        {
            if (transaction?.Connection == null)
                throw new ArgumentNullException(nameof(transaction));

            var query = @"
            SELECT 1
            FROM ""TMS"".""Trans_Trip""
            WHERE ""vehicle_no"" = @VehicleNumber
            AND (
                ""etd"" < @PlannedEndTime
                AND ""rta"" > @PlannedStartTime
            )
            LIMIT 1;
            ";

            var result = await transaction.Connection.ExecuteScalarAsync<int?>(
                query,
                new
                {
                    VehicleNumber = vehicleNumber,
                    PlannedStartTime = plannedStartTime,
                    PlannedEndTime = plannedEndTime
                },
                transaction: transaction
            );

            return result.HasValue;
        }


        public async Task<List<TripPlanRouteDetailsDTO>> GetRouteDetailsByPlanIdAsync(int planId,IDbTransaction transaction)
        {

            string sql = """
            SELECT 
                plan_id AS planId, from_geo_id AS fromGeoId, to_geo_id AS toGeoId,
                sequence, distance, lead_time AS leadTime, "RTA" AS rta,
                to_entry_time AS toEntryTime, from_exit_time AS fromExitTime,
                google_suggested_time AS googleSuggestedTime
            FROM "TMS"."Trip_Plan_Route_Detail"
            WHERE plan_id = @Id
            ORDER BY sequence ASC
            """;

            var result = await transaction.Connection.QueryAsync<TripPlanRouteDetailsDTO>(sql, new { Id = planId }, transaction: transaction);
            return result.ToList();
        }

        public async Task CreateTransAndDetTripAsync(
        int planId,
        CreateTripDTO request,
        List<TripPlanRouteDetailsDTO> segments,
        DateTime TripETD,
        DateTime TripRTA,
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
            start_geo_id, end_geo_id,created_by,
            trip_type, primary_device, consignee, consignor,
            secondary_devices, vehicle_category, routing_model, route_path
            )
            VALUES
            (
            @AccountId, @DriverId, @VehicleId, @TripType, 
            @TravelDate, @ETD, @RTA, @TotalLeadTime, 
            @RouteId, @CreatedDatetime, true,
            @driverName, @vehicleNumber, @driverPhone,
            @StartGeoId, @EndGeoId,@CreatedBy,
            @TripType, @PrimaryDevice, @Consignee, @Consignor,
            @SecondaryDevice::jsonb, 
            @VehicleCategory, @RoutingModel, @RoutePath
            )
            RETURNING trip_id;
            """;

            // We calculate the final RTA after processing details, but for the insert 
            // we'll update it or calculate it upfront. Let's calculate the timeline.
            DateTime currentTimeline = DateTime.SpecifyKind(TripETD, DateTimeKind.Utc);


            int transTripId = await transaction.Connection.ExecuteScalarAsync<int>(transSql, new
            {
                request.accountId,
                request.driverId,
                request.vehicleId,
                TripType = request.frequency,
                TravelDate = currentTimeline.Date,
                ETD = TripETD,
                RTA = TripRTA,
                TotalLeadTime = request.googleSuggestedTime,
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
                SecondaryDevice = request.secondaryDevices,
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
                TripId = transTripId,
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
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd hh:mm:ss",
                "yyyy-MM-dd hh:mm:ss tt",
                "yyyy-MM-dd HH:mm",
                "dd-MM-yyyy HH:mm:ss",
                "dd-MM-yyyy hh:mm:ss",
                "dd-MM-yyyy hh:mm:ss tt",
                "dd-MM-yyyy HH:mm",
                "MM/dd/yyyy hh:mm:ss tt",
                "MM/dd/yyyy HH:mm:ss",
                "MM/dd/yyyy HH:mm",
                "MM/dd/yyyy hh:mm tt",
                "MM-dd-yyyy hh:mm:ss tt",
                "MM-dd-yyyy HH:mm:ss",
                "MM-dd-yyyy HH:mm",
                "MM-dd-yyyy hh:mm tt"
            };

            return DateTime.ParseExact(
            dateTime,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None);
        }

    }
}

    
