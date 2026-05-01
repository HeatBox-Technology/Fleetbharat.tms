using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Application.Interfaces;
using FleetBharat.TMSService.Infrastructure.ConnectionFactory;
using FleetBharat.TMSService.Infrastructure.ExternalServices;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;
using Newtonsoft.Json;

namespace FleetBharat.TMSService.Application.Services
{
    public class TripSyncService : ITripSyncService
    {
        private readonly ITripSyncRepository _tripSyncRepository;
        private readonly ILogger<TripSyncService> _logger;
        private readonly JavaServicesApiClient _httpClient;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        public TripSyncService(ITripSyncRepository tripSyncRepository, ILogger<TripSyncService> logger, 
            JavaServicesApiClient httpClient, IDbConnectionFactory dbConnectionFactory)
        {
            _tripSyncRepository = tripSyncRepository;
            _logger = logger;
            _httpClient = httpClient;
            _dbConnectionFactory = dbConnectionFactory;
        }
        public async Task SyncTripsAsync()
        {
            _logger.LogInformation("Trip sync job started");

            //Step 1: Fetch unsynced trips
            var rows = await _tripSyncRepository.GetCurrentUnsyncedTripsAsync();

            if (!rows.Any())
            {
                _logger.LogInformation("No trips to sync");
                return;
            }

            var trips = new List<TripSyncDTO>();

            foreach (var row in rows)
            {
                var trip = new TripSyncDTO
                {
                    tripId = row.trip_id,
                    tripName = row.trip_name,
                    vehicleId = row.vehicle_no,
                    vehicleNo = row.vehicle_no,
                    deviceNo = row.device_no,
                    orgId = row.account_id,
                    orgName = "",
                    tripStartTime = row.trip_start_time,
                    tripEndTime = row.trip_end_time,
                    encodedRoute = row.encoded_route,

                    geofenceList = MapGeofences(row.geofence_json),

                    secondaryDevices = string.IsNullOrEmpty(row.secondary_devices_json)
                        ? new List<string>()
                        : JsonConvert.DeserializeObject<List<string>>(row.secondary_devices_json)
                };

                trips.Add(trip);
            }
            //Step 2: Call external API
            bool response = await _httpClient.SendTripMappingAsync(trips);

            if (!response)
            {
                _logger.LogError("API call failed: {Status}","Trip Sync API Failed");
                throw new Exception("Trip sync API failed");
            }

            //Step 3: Mark as synced (ONLY AFTER SUCCESS)
            using var connection = _dbConnectionFactory.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                var tripIds = trips.Select(t => t.tripId).ToList();

                var updated = await _tripSyncRepository.MarkTripsAsSyncedAsync(
                    transaction,
                    tripIds
                );

                transaction.Commit();

                _logger.LogInformation("Synced {Count} trips", updated);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Failed to mark trips as synced");
                throw;
            }


        }

        private List<GeofenceSyncDTO> MapGeofences(string geofenceJson)
        {
            if (string.IsNullOrWhiteSpace(geofenceJson))
                return new List<GeofenceSyncDTO>();

            var rawList = JsonConvert.DeserializeObject<List<GeofenceRawDTO>>(geofenceJson);

            return rawList.Select(x =>
            {
                var geo = new GeofenceSyncDTO
                {
                    geoId = x.geofenceId.ToString(),
                    geoName = x.geofenceId.ToString(), // adjust if name available
                    pointType = x.pointType,
                    geoType = x.geofenceType,

                    radius = int.TryParse(x.geofenceRadius, out var r) ? r : 0,

                    latitude = decimal.TryParse(x.geofenceCenterLatitude, out var lat) ? lat : 0,
                    longitude = decimal.TryParse(x.geofenceCenterLongitude, out var lng) ? lng : 0,

                    GeoPoints = new List<GeoPointSyncDTO>()
                };

                //Handle polygon (if data comes later)
                if (x.geofenceType == "POLYGON" && !string.IsNullOrEmpty(x.geofenceDetails))
                {
                    try
                    {
                        geo.GeoPoints = JsonConvert.DeserializeObject<List<GeoPointSyncDTO>>(x.geofenceDetails);
                    }
                    catch
                    {
                        geo.GeoPoints = new List<GeoPointSyncDTO>();
                    }
                }

                return geo;
            }).ToList();
        }
    }
}
