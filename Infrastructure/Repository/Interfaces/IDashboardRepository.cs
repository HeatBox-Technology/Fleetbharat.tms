using FleetBharat.TMSService.Application.DTOs;

namespace FleetBharat.TMSService.Infrastructure.Repository.Interfaces
{
    public interface IDashboardRepository
    {
        Task<TripDashboardDto> GetTripDashboardAsync();
    }
}
