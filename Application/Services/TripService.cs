using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Application.Helper;
using FleetBharat.TMSService.Domain.Entities.TMS;
using FleetBharat.TMSService.Infrastructure.ConnectionFactory;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;
using Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

public class TripService : ITripService
{
    private readonly AppDbContext _dbContext;
    private readonly ITripPlanRepository _tripPlanRepository;
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly CommonApiClient _commonApi;
    public TripService(ITripPlanRepository tripPlanRepository, AppDbContext dbContext, IDbConnectionFactory dbConnectionFactory, CommonApiClient commonApi)
    {
        _dbContext = dbContext;
        _tripPlanRepository = tripPlanRepository;
        _dbConnectionFactory = dbConnectionFactory;
        _commonApi = commonApi;
    }

    public async Task<ApiResponse<int>> CreateTripPlanAsync(TripPlanRequestDTO request)
    {
        if (request.routeDetails == null || !request.routeDetails.Any())
            return ApiResponse<int>.Fail("Route details are required.", 500);

        using var connection = _dbConnectionFactory.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            int totalLeadTime = request.routeDetails.Sum(x => x.leadTime);
            int totalETA = request.routeDetails.Sum(x => x.rta);

            DateTime? parsedTravelDate = null;

            if (request.tripType.Equals("fixed", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(request.travelDate))
            {
                DateTime.TryParseExact(
                    request.travelDate,
                    "dd/MM/yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date);

                parsedTravelDate = date;
            }

            int planId = await _tripPlanRepository.CreateTripPlanAsync(
                request,
                parsedTravelDate,
                totalLeadTime,
                totalETA,
                transaction);

            await _tripPlanRepository.InsertRouteDetailsAsync(
                planId,
                request.routeDetails,
                transaction);

            if (request.tripType.Equals("fixed", StringComparison.OrdinalIgnoreCase))
            {
                // Parse start date/time
                var datePart = DatetimeHelper.ParseToDate(request.travelDate, ["dd/MM/yyyy"]) ?? DateTime.UtcNow;
                TimeSpan.TryParse(request.etd, out var timePart);
                DateTime baseTimeline = datePart.Date.Add(timePart);

                await _tripPlanRepository.CreateTransAndDetTripAsync(planId, request, baseTimeline, transaction);
            }

            transaction.Commit();

            return ApiResponse<int>.Ok(planId, "Trip plan saved successfully");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return ApiResponse<int>.Fail($"Error saving trip plan: {ex.Message}", 500);
        }
    }

    public async Task<TripPlanListUiResponseDto> GetTripPlanListAsync(int accountId, int page = 1, int pageSize = 20)
    {
        // 1. Get raw data from Repository
        var (rawTrips, total, totalActive) = await _tripPlanRepository.GetAllTripPlansAsync(accountId, page, pageSize);

        var response = new TripPlanListUiResponseDto();
        response.Summary.TotalRecords = total;
        response.Summary.TotalActive = totalActive;
        response.Summary.TotalInactive = total - totalActive;

        if (!rawTrips.Any())
        {
            response.Data = new PagedResultDto<TripPlanResponseDTO>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                Items = new List<TripPlanResponseDTO>()
            };
            return response;
        }

        // 2. Fetch External Metadata (Geofences/Accounts)
        var geofences = await _commonApi.GetGeofencesAsync(accountId, 100) ?? new();
        var accounts = await _commonApi.GetAccountsAsync(200) ?? new();
        var vehicles = await _commonApi.GetVehicleAsync(accountId) ?? new();

        // 3. Map to Response DTO
        var items = rawTrips.Select(trip => new TripPlanResponseDTO
        {
            planId = trip.plan_id ?? 0,
            accountId = trip.account_id ?? 0,
            // Cast to int? before comparing to handle null IDs safely
            accountName = accounts.FirstOrDefault(a => a.id == (int?)(trip.account_id))?.value,
            driverId = trip.driver_id ?? 0,
            driverName=trip.driver_name,
            vehicleId = trip.vehicle_id ?? 0,
            vehicleNo= trip.vehicle_no,
            tripType = trip.trip_type,
            travelDate = trip.travel_date?.ToString("dd/MM/yyyy"),
            etd = trip.ETD,
            leadTime = trip.lead_time ?? 0,
            eta = trip.ETA ?? 0,
            routeId = trip.route_id ?? 0,
            routeName = trip.route_name,
            startGeoId = trip.start_geo_id ?? 0,
            startGeoName = geofences.FirstOrDefault(g => g.id == (int?)(trip.start_geo_id))?.value,
            endGeoId = trip.end_geo_id ?? 0,
            endGeoName = geofences.FirstOrDefault(g => g.id == (int?)(trip.end_geo_id))?.value,
            createdDatetime = trip.created_datetime ?? DateTime.MinValue,
            isActive = trip.is_active ?? false
        }).ToList();

        response.Data = new PagedResultDto<TripPlanResponseDTO>
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Items = items
        };

        return response;
    }

    public async Task<ApiResponse<bool>> DeleteTripPlanAsync(int planId)
    {
        
        bool isDeleted = await _tripPlanRepository.DeleteTripPlanAsync(planId);

        if (!isDeleted)
        {
            throw new KeyNotFoundException("Trip Plan not found");
        }

        return ApiResponse<bool>.Ok(true, "Trip Plan and associated plan details deleted successfully.");
        
    }

    public async Task<ApiResponse<bool>> UpdateTripPlanAsync(TripPlanRequestDTO request)
    {
        if (request.routeDetails == null || !request.routeDetails.Any())
            throw new BadHttpRequestException("Route details are required.");

        using var connection = _dbConnectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 1. Calculate Totals
            int totalLeadTime = request.routeDetails.Sum(x => x.leadTime);
            int totalETA = request.routeDetails.Sum(x => x.rta);

            DateTime? parsedTravelDate = null;
            if (request.tripType.Equals("fixed", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(request.travelDate))
            {
                if (DateTime.TryParseExact(request.travelDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    parsedTravelDate = date;
            }

            // 2. Update Main Plan
            bool updated = await _tripPlanRepository.UpdateTripPlanAsync(
                request.planId, request, parsedTravelDate, totalLeadTime, totalETA, transaction);

            if (!updated)
                throw new KeyNotFoundException($"Trip Plan not found.");

            // 3. Clear old Route Details and Insert New Ones
            await _tripPlanRepository.DeleteRouteDetailsByPlanIdAsync(request.planId, transaction);
            await _tripPlanRepository.InsertRouteDetailsAsync(request.planId, request.routeDetails, transaction);

            if (request.tripType.Equals("fixed", StringComparison.OrdinalIgnoreCase))
            {
                var datePart = DatetimeHelper.ParseToDate(request.travelDate, ["dd/MM/yyyy"]) ?? DateTime.UtcNow;
                TimeSpan.TryParse(request.etd, out var timePart);
                DateTime baseTimeline = datePart.Date.Add(timePart);

                await _tripPlanRepository.CreateTransAndDetTripAsync(request.planId, request, baseTimeline, transaction);
            }

            transaction.Commit();
            return ApiResponse<bool>.Ok(true, "Trip plan updated successfully");
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<ApiResponse<TripPlanByIdResponseDTO>> GetTripByIdAsync(int planId)
    {
        // 1. Fetch from repository
        var plan = await _tripPlanRepository.GetTripPlanByIdAsync(planId);

        // 2. Handle "Not Found" via Middleware
        if (plan == null)
        {
            throw new KeyNotFoundException($"Trip Plan not found.");
        }

        // 3. Fetch the child records separately
        var details = await _tripPlanRepository.GetRouteDetailsByPlanIdAsync(planId);

        // 4. Combine them
        plan.routeDetails = details.ToList();

        return ApiResponse<TripPlanByIdResponseDTO>.Ok(plan, "Trip details retrieved.");
    }
}