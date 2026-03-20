using FleetBharat.TMSService.Application.DTOs;

public interface ITripService
{
    Task<ApiResponse<int>> CreateTripPlanAsync(TripPlanRequestDTO request);
    Task<TripPlanListUiResponseDto> GetTripPlanListAsync(int accountId, int page = 1, int pageSize = 20);
    Task<ApiResponse<bool>> DeleteTripPlanAsync(int planId);
    Task<ApiResponse<bool>> UpdateTripPlanAsync(TripPlanRequestDTO request);
    Task<ApiResponse<TripPlanByIdResponseDTO>> GetTripByIdAsync(int planId);
}