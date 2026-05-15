using FleetBharat.TMSService.Application.DTOs;

namespace FleetBharat.TMSService.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<TripDashboardDto> GetTripDashboardAsync(int accountId);
    }
}
