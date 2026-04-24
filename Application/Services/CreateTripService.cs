using FleetBharat.TMSService.Application.Interfaces;
using FleetBharat.TMSService.Infrastructure.ConnectionFactory;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;
using Hangfire;
using System.Collections;

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

                    var plannedStart = today.Add(TimeSpan.Parse(trip.plannedStartTime));
                    var googelSuggestedEnd = plannedStart.AddMinutes(trip.googleSuggestedTime);
                    var plannedEnd = googelSuggestedEnd.Date.Add(TimeSpan.Parse(trip.plannedEndTime));

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
                            var segment_etd = today.Add(TimeSpan.Parse(route.fromExitTime));
                            var segment_eta = segment_etd.AddMinutes(trip.googleSuggestedTime);
                            var segment_rta = googelSuggestedEnd.Date.Add(TimeSpan.Parse(route.toEntryTime));

                            route.fromExitTime = segment_etd.ToString();
                            route.toEntryTime = segment_rta.ToString();
                        }

                        await _createTripRepository.CreateTransAndDetTripAsync(
                        trip.planId,
                        trip,
                        routeDetails,
                        plannedStart,
                        plannedEnd,
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
