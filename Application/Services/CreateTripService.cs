using FleetBharat.TMSService.Application.Interfaces;
using FleetBharat.TMSService.Infrastructure.ConnectionFactory;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;
using Hangfire;
using Newtonsoft.Json;
using System.Collections;
using System.Globalization;

namespace FleetBharat.TMSService.Application.Services
{
    public class CreateTripService : ICreateTripService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly AppDbContext _dbContext;
        private readonly ICreateTripRepository _createTripRepository;

        public CreateTripService(IDbConnectionFactory dbConnectionFactory, AppDbContext dbContext, ICreateTripRepository createTripRepository)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _dbContext = dbContext;
            _createTripRepository = createTripRepository;
        }

        [JobDisplayName("Create Daily Trips")]
        [AutomaticRetry(Attempts = 3)]
        [DisableConcurrentExecution(3600)]
        public async Task CreateTripAsync()
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                var today = TimeZoneInfo
                            .ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"))
                            .Date;

                var recurringTrips = await _createTripRepository.GetRecurringTrips(transaction);
                
                foreach(var trip in recurringTrips)
                {
                    var days = trip.weekDays?.Split(',').Select(d => d.Trim().ToUpper());
                    if (days == null || !days.Contains(today.DayOfWeek.ToString().ToUpper()))
                        continue;

                    //var plannedStart = today.Add(TimeSpan.Parse(trip.plannedStartTime));
                    var plannedStart = today.Add(
                                        DateTime.ParseExact(
                                            trip.plannedStartTime,
                                            "hh:mm tt",
                                            CultureInfo.InvariantCulture
                                        ).TimeOfDay);

                    var googelSuggestedEnd = plannedStart.AddMinutes(trip.googleSuggestedTime);
                    var plannedEnd = googelSuggestedEnd.Date.Add(
                                        DateTime.ParseExact(
                                            trip.plannedEndTime,
                                            "hh:mm tt",
                                            CultureInfo.InvariantCulture
                                        ).TimeOfDay);

                    var exists = await _createTripRepository.TripExistsAsync(
                    transaction,
                    trip.vehicleNumber,
                    plannedStart,
                    plannedEnd);

                    if (exists)
                        continue;
                    
                    var routeDetails = await _createTripRepository.GetRouteDetailsByPlanIdAsync(trip.planId, transaction);

                    if (routeDetails != null)
                    {
                        routeDetails = routeDetails.OrderBy(r => r.sequence).ToList();

                        foreach (var route in routeDetails)
                        {
                            var segment_etd = today.Add(
                                        DateTime.ParseExact(
                                            route.fromExitTime,
                                            "hh:mm tt",
                                            CultureInfo.InvariantCulture
                                        ).TimeOfDay);
                            var segment_eta = segment_etd.AddMinutes(trip.googleSuggestedTime);
                            var segment_rta = googelSuggestedEnd.Date.Add(DateTime.ParseExact(
                                            route.toEntryTime,
                                            "hh:mm tt",
                                            CultureInfo.InvariantCulture
                                        ).TimeOfDay);

                            route.fromExitTime = segment_etd.ToString();
                            route.toEntryTime = segment_rta.ToString();
                        }

                        var geofenceDetails = await _createTripRepository.GetGeofenceDetailsByPlanIdAsync(trip.planId);

                        var geofenceList = geofenceDetails?.OrderBy(x => x.sequence).ToList();

                        // ✅ Convert to JSON
                        var geofenceJson = (geofenceList == null || !geofenceList.Any())
                            ? "[]"
                            : JsonConvert.SerializeObject(geofenceList);

                        await _createTripRepository.CreateTransAndDetTripAsync(
                        trip.planId,
                        trip,
                        routeDetails,
                        plannedStart,
                        plannedEnd,
                        geofenceJson,
                        transaction);

                        transaction.Commit();
                    }

                                        

                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw ex;
            } 

        }
    }
}
