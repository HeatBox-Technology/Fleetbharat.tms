using FleetBharat.TMSService.Application.DTOs;

namespace FleetBharat.TMSService.Infrastructure.Repository.Interfaces
{
    public interface ITripReportRepository
    {
        Task<(TripReportSummaryDto Summary, IEnumerable<TripReportDbRowDto> Items, int TotalRecords)> GetTripReportAsync(TripReportRepositoryRequestDto request);
    }
}
