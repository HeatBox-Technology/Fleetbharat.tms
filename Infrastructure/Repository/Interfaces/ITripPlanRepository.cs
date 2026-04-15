using FleetBharat.TMSService.Application.DTOs;
using System.Data;

namespace FleetBharat.TMSService.Infrastructure.Repository.Interfaces
{
    public interface ITripPlanRepository
    {
        Task<int> CreateTripPlanAsync(TripPlanRequestDTO request,
            DateTime? travelDate,
            int eta,
            string secondaryDevicesJson,
            string plannedEntryTime,
            string plannedExitTime,
            IDbTransaction transaction);

        Task InsertRouteDetailsAsync(
        int planId,
        IEnumerable<TripPlanRouteDetailsDTO> routeDetails,
        IDbTransaction transaction);

        Task InsertGeofencePointsAsync(
        int planId,
        List<TripPlanGeofenceRouteDetailsDTO> geofenceDetails,
        IDbTransaction transaction);


        Task CreateTransAndDetTripAsync(
        int planId,
        TripPlanRequestDTO request,
        List<TripPlanRouteDetailsDTO> segments,
        DateTime TripETD,
        DateTime TripRTA,
        string secondaryDevicesJson,
        IDbTransaction transaction);

        Task<(IEnumerable<dynamic> Items, int Total, int TotalActive)> GetAllTripPlansAsync
            (int accountId, int page, int pageSize);

        Task<bool> DeleteTripPlanAsync(int planId);

        Task<bool> UpdateTripPlanAsync(
        int planId,
        TripPlanRequestDTO request,
        DateTime? travelDate,
        int eta,
        string secondaryDevicesJson,
        string plannedEntryTime,
        string plannedExitTime,
        IDbTransaction transaction);

        Task DeleteRouteDetailsByPlanIdAsync(int planId, IDbTransaction transaction);

        Task DeleteGeofenceDetailsByPlanIdAsync(int planId, IDbTransaction transaction);

        Task<TripPlanByIdResponseDTO?> GetTripPlanByIdAsync(int planId);

        Task<IEnumerable<TripPlanRouteDetailsDTO>> GetRouteDetailsByPlanIdAsync(int planId);

        Task<IEnumerable<TripPlanGeofenceDbResponseDTO>> GetGeofenceDetailsByPlanIdAsync(int planId);
    }
}
