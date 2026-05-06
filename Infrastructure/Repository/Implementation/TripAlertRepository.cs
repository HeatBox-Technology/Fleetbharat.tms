using Dapper;
using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;
using System.Data;

namespace FleetBharat.TMSService.Infrastructure.Repository.Implementation
{
    public class TripAlertRepository : ITripAlertRepository
    {
        public async Task InsertAlertAsync(IDbTransaction transaction, TripAlertDTO alert)
        {
            var sql = @"
            INSERT INTO ""TMS"".""trip_alerts"" (
                org_id, org_name, vehicle_id, vehicle_no, device_no,
                gps_date, geo_id, latitude, longitude, address,
                geo_name, geo_status, received_at,
                trip_id, trip_name, point_type
            )
            VALUES (
                @OrgId, @OrgName, @VehicleId, @VehicleNo, @DeviceNo,
                @GpsDate, @GeoId, @Latitude, @Longitude, @Address,
                @GeoName, @GeoStatus, @ReceivedAt,
                @TripId, @TripName, @PointType
            )";

            await transaction.Connection.ExecuteAsync(sql, alert, transaction);
        }

        public async Task HandleStartAsync(IDbTransaction transaction, TripAlertDTO alert, string geoStatus)
        {
            if (geoStatus == "ENTER")
            {
                var sql = @"
                UPDATE ""TMS"".""Trans_Trip""
                SET start_in_datetime = @GpsDate
                WHERE trip_id = @TripId
                AND (start_in_datetime IS NULL OR @GpsDate < start_in_datetime)
                AND start_geo_id = CAST(@GeoId AS INTEGER);

                UPDATE ""TMS"".""Det_Trip""
                SET from_in_datetime = @GpsDate
                WHERE trip_id = @TripId
                AND from_geo_id= CAST(@GeoId AS INTEGER)
                AND (from_in_datetime IS NULL OR @GpsDate < from_in_datetime);
                ";

                await transaction.Connection.ExecuteAsync(sql, alert, transaction);
            }
            else if (geoStatus == "EXIT")
            {
                var sql = @"
                UPDATE ""TMS"".""Trans_Trip""
                SET 
                    start_out_datetime = @GpsDate,
                    atd = @GpsDate,
                    trip_status = 'TS'
                WHERE trip_id = @TripId
                AND start_geo_id = CAST(NULLIF(@GeoId, '') AS INTEGER)
                AND (start_out_datetime IS NULL OR @GpsDate > start_out_datetime);

                UPDATE ""TMS"".""Det_Trip""
                SET 
                    from_out_datetime = @GpsDate
                WHERE trip_id = @TripId AND from_geo_id = CAST(NULLIF(@GeoId, '') AS INTEGER)
                AND (from_out_datetime IS NULL OR @GpsDate > from_out_datetime);
                ";

                await transaction.Connection.ExecuteAsync(sql, alert, transaction);
            }
        }

        public async Task HandleViaAsync(IDbTransaction transaction, TripAlertDTO alert, string geoStatus)
        {
            if (geoStatus == "ENTER")
            {
                var sql = @"
                UPDATE ""TMS"".""Det_Trip""
                SET from_in_datetime = @GpsDate
                WHERE trip_id = @TripId AND from_geo_id = CAST(NULLIF(@GeoId, '') AS INTEGER)
                AND (from_in_datetime IS NULL OR @GpsDate < from_in_datetime);

                UPDATE ""TMS"".""Det_Trip""
                SET to_in_datetime = @GpsDate
                WHERE trip_id = @TripId AND to_geo_id = CAST(NULLIF(@GeoId, '') AS INTEGER)
                AND (to_in_datetime IS NULL OR @GpsDate < to_in_datetime);
                ";

                await transaction.Connection.ExecuteAsync(sql, alert, transaction);
            }
            else if (geoStatus == "EXIT")
            {
                var sql = @"
                UPDATE ""TMS"".""Det_Trip""
                SET from_out_datetime = @GpsDate
                WHERE trip_id = @TripId AND from_geo_id = CAST(NULLIF(@GeoId, '') AS INTEGER)
                AND (from_out_datetime IS NULL OR @GpsDate > from_out_datetime);

                UPDATE ""TMS"".""Det_Trip""
                SET to_out_datetime = @GpsDate
                WHERE trip_id = @TripId AND to_geo_id = CAST(NULLIF(@GeoId, '') AS INTEGER)
                AND (to_out_datetime IS NULL OR @GpsDate > to_out_datetime);
                ";

                await transaction.Connection.ExecuteAsync(sql, alert, transaction);
            }
        }

        public async Task HandleEndAsync(IDbTransaction transaction, TripAlertDTO alert, string geoStatus)
        {
            if (geoStatus == "ENTER")
            {
                var sql = @"
                UPDATE ""TMS"".""Trans_Trip""
                SET 
                    end_in_datetime = @GpsDate,
                    ata = @GpsDate,
                    trip_status = 'TE'
                WHERE trip_id = @TripId
                AND end_geo_id = CAST(NULLIF(@GeoId, '') AS INTEGER)
                AND (end_in_datetime IS NULL OR @GpsDate < end_in_datetime)
                AND trip_status='TS';

                UPDATE ""TMS"".""Det_Trip""
                SET to_in_datetime = @GpsDate
                WHERE trip_id = @TripId AND to_geo_id = CAST(NULLIF(@GeoId, '') AS INTEGER)
                AND (to_in_datetime IS NULL OR @GpsDate < to_in_datetime);
                ";

                await transaction.Connection.ExecuteAsync(sql, alert, transaction);
            }
            else if (geoStatus == "EXIT")
            {
                var sql = @"
                UPDATE ""TMS"".""Trans_Trip""
                SET 
                    end_out_datetime = @GpsDate,
                    trip_completed = true
                WHERE trip_id = @TripId
                AND end_geo_id = CAST(NULLIF(@GeoId, '') AS INTEGER)
                AND (end_out_datetime IS NULL OR @GpsDate > end_out_datetime);

                UPDATE ""TMS"".""Det_Trip""
                SET to_out_datetime = @GpsDate
                WHERE trip_id = @TripId AND to_geo_id = CAST(NULLIF(@GeoId, '') AS INTEGER)
                AND (to_out_datetime IS NULL OR @GpsDate > to_out_datetime);
                ";

                await transaction.Connection.ExecuteAsync(sql, alert, transaction);
            }
        }
    }
}