using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Application.Interfaces;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;

namespace FleetBharat.TMSService.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardService(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public Task<TripDashboardDto> GetTripDashboardAsync()
        {
            return _dashboardRepository.GetTripDashboardAsync();
        }
    }
}
