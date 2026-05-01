using Dapper;
using FleetBharat.TMSService.Infrastructure.ConnectionFactory;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;
using System.Data;

namespace FleetBharat.TMSService.Infrastructure.Repository.Implementation
{
    public class TripSyncRepository : ITripSyncRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public TripSyncRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async Task<IEnumerable<dynamic>> GetCurrentUnsyncedTripsAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            var sql = @"
            SELECT 
                trip_id,
                trip_no as trip_name,
                vehicle_no,
                primary_device as device_no,
                account_id,
                etd as trip_start_time,
                rta as trip_end_time,
                route_path as encoded_route,
                geofence_json,
                secondary_devices
            FROM ""TMS"".""Trans_Trip""
            WHERE is_current_trip = true
            AND COALESCE(trip_synced, false) = false;
            ";

            return await connection.QueryAsync(sql);
        }

        public async Task<int> MarkTripsAsSyncedAsync(
        IDbTransaction transaction,
        List<int> tripIds)
        {
            var sql = @"
            UPDATE ""TMS"".""Trans_Trip""
            SET 
                trip_synced = true,
                trip_sync_datetime = @UtcNow
            WHERE trip_id = ANY(@TripIds);
            ";

            return await transaction.Connection.ExecuteAsync(sql, new
            {
                TripIds = tripIds,
                UtcNow = DateTime.UtcNow
            }, transaction);
        }
    }
}
