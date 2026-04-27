using Dapper;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;
using System.Data;

namespace FleetBharat.TMSService.Infrastructure.Repository.Implementation
{
    public class CurrentTripRepository : ICurrentTripRepository
    {
        public async Task<int> UpdateCurrentTripAndLegentIcon(IDbTransaction transaction)
        {
            var connection = transaction.Connection;
            // ✅ Query 1: Assign Current Trip (ONLY for eligible trips)
            var currentTripSql = @"
            WITH vehicles_with_current AS (
                SELECT DISTINCT vehicle_no
                FROM ""TMS"".""Trans_Trip""
                WHERE is_current_trip = true
            ),
            ranked_trips AS (
                SELECT 
                    trip_id,
                    vehicle_no,
                    ROW_NUMBER() OVER (
                        PARTITION BY vehicle_no
                        ORDER BY etd ASC, trip_id ASC
                    ) AS ranked
                FROM ""TMS"".""Trans_Trip""
                WHERE COALESCE(trip_status, '') = ''
            )
            UPDATE ""TMS"".""Trans_Trip"" t
            SET is_current_trip = true
            FROM ranked_trips r
            WHERE t.trip_id = r.trip_id
            AND r.ranked = 1
            AND NOT EXISTS (
                SELECT 1 
                FROM vehicles_with_current v 
                WHERE v.vehicle_no = t.vehicle_no
            )
            AND t.is_current_trip IS DISTINCT FROM true;
        ";

            // ✅ Query 2: Update Legend (ALL trips, including TS)
            var legendSql = @"
            UPDATE ""TMS"".""Trans_Trip""
            SET 
                legend_status = CASE
                    WHEN trip_status IS NULL THEN 'NOT_YET_STARTED'

                    WHEN atd IS NOT NULL AND atd <= etd THEN 'ON_TIME'
                    WHEN atd IS NOT NULL AND atd > etd THEN 'DELAYED'

                    WHEN atd IS NULL AND NOW() <= etd THEN 'ON_TIME'
                    ELSE 'DELAYED'
                END,

                legend_icon = CASE
                    WHEN trip_status IS NULL THEN 'grey'

                    WHEN atd IS NOT NULL AND atd <= etd THEN 'green'
                    WHEN atd IS NOT NULL AND atd > etd THEN 'red'

                    WHEN atd IS NULL AND NOW() <= etd THEN 'green'
                    ELSE 'red'
                END
            WHERE 
                legend_status IS DISTINCT FROM 
                    CASE
                        WHEN trip_status IS NULL THEN 'NOT_YET_STARTED'
                        WHEN atd IS NOT NULL AND atd <= etd THEN 'ON_TIME'
                        WHEN atd IS NOT NULL AND atd > etd THEN 'DELAYED'
                        WHEN atd IS NULL AND NOW() <= etd THEN 'ON_TIME'
                        ELSE 'DELAYED'
                    END
                OR
                legend_icon IS DISTINCT FROM 
                    CASE
                        WHEN trip_status IS NULL THEN 'grey'
                        WHEN atd IS NOT NULL AND atd <= etd THEN 'green'
                        WHEN atd IS NOT NULL AND atd > etd THEN 'red'
                        WHEN atd IS NULL AND NOW() <= etd THEN 'green'
                        ELSE 'red'
                    END;
        ";

            // Execute both queries in same transaction
            var rows1 = await connection.ExecuteAsync(currentTripSql, transaction: transaction);
            var rows2 = await connection.ExecuteAsync(legendSql, transaction: transaction);

            return rows1 + rows2;
        }
    }
}
