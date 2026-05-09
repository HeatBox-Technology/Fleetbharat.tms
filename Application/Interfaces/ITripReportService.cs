using FleetBharat.TMSService.Application.DTOs;

namespace FleetBharat.TMSService.Application.Interfaces
{
    public interface ITripReportService
    {
        Task<ApiResponse<TripReportListUiResponseDto>> GetTripReportAsync(TripReportFilterDto request);
    }
}
