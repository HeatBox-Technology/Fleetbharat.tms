using FleetBharat.TMSService.Application.DTOs;
using System.Data;

namespace FleetBharat.TMSService.Infrastructure.Repository.Interfaces
{
    public interface ICreateTripRepository
    {
        Task<List<CreateTripDTO>> GetRecurringTrips(IDbTransaction transaction);
        Task<bool> TripExistsAsync(IDbTransaction transaction, string vehicleNumber, DateTime plannedStartTime, DateTime plannedEndTime);

        Task<List<TripPlanRouteDetailsDTO>> GetRouteDetailsByPlanIdAsync(int planId, IDbTransaction transaction);
        Task CreateTransAndDetTripAsync(
        int planId,
        CreateTripDTO request,
        List<TripPlanRouteDetailsDTO> segments,
        DateTime TripETD,
        DateTime TripRTA,
        IDbTransaction transaction);
    }
}
