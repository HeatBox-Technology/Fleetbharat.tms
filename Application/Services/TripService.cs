using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Application.Helper;
using FleetBharat.TMSService.Domain.Entities.TMS;
using FleetBharat.TMSService.Infrastructure.ConnectionFactory;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;
using Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using System.Data;
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
        // ✅ Step 1: Validate Request
        var validationResult = ValidateRequest(request);
        if (!validationResult.success)
            return validationResult;


        using var connection = _dbConnectionFactory.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            if (IsDynamicRouting(request.routingModel))
            {
                await CreateGeofencesAndMapAsync(request, transaction);
            }

            int totalLeadTime = request.routeDetails.Sum(x => x.leadTime);
            int totalETA = request.routeDetails.Sum(x => x.rta);

            DateTime? parsedTravelDate = null;

            if (IsOneTime(request.frequency))
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

            if (IsOneTime(request.frequency))
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

    #region Validation

    private ApiResponse<int> ValidateRequest(TripPlanRequestDTO request)
    {
        if (request == null)
            return ApiResponse<int>.Fail("Request cannot be null", 400);

        if (request.routeDetails == null || !request.routeDetails.Any())
            return ApiResponse<int>.Fail("Route details are required.", 400);

        if (string.IsNullOrWhiteSpace(request.frequency))
            return ApiResponse<int>.Fail("Frequency is required.", 400);

        if (!IsValidFrequency(request.frequency))
            return ApiResponse<int>.Fail("Invalid frequency value.", 400);

        if (string.IsNullOrWhiteSpace(request.etd))
            return ApiResponse<int>.Fail("ETD is required.", 400);

        if (!TimeSpan.TryParse(request.etd, out _))
            return ApiResponse<int>.Fail("Invalid ETD format (HH:mm expected).", 400);

        // ✅ One-Time Validation
        if (IsOneTime(request.frequency))
        {
            if (string.IsNullOrWhiteSpace(request.travelDate))
                return ApiResponse<int>.Fail("Travel date is required for one-time trips.", 400);

            if (!DateTime.TryParseExact(request.travelDate, "dd/MM/yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return ApiResponse<int>.Fail("Invalid travel date format. Use dd/MM/yyyy", 400);
            }
        }

        // ✅ Recurring Validation
        if (IsRecurring(request.frequency))
        {
            if (string.IsNullOrWhiteSpace(request.weekDays))
                return ApiResponse<int>.Fail("WeekDays required for recurring trips.", 400);
        }

        // ✅ Fleet Validation
        if (IsInternalFleet(request.fleetSource))
        {
            if (request.vehicleId == 0)
                return ApiResponse<int>.Fail("VehicleId required for internal fleet.", 400);
        }
        else if (IsExternalFleet(request.fleetSource))
        {
            if (string.IsNullOrWhiteSpace(request.vehicleNumber))
                return ApiResponse<int>.Fail("Vehicle number required for external fleet.", 400);
        }

        if (IsDynamicRouting(request.routingModel))
        {
            foreach (var route in request.routeDetails)
            {
                if (string.IsNullOrWhiteSpace(route.fromGeoName) ||
                    string.IsNullOrWhiteSpace(route.fromLatitude) ||
                    string.IsNullOrWhiteSpace(route.fromLongitude))
                {
                    return ApiResponse<int>.Fail("Invalid FROM geofence data for dynamic routing", 400);
                }

                if (string.IsNullOrWhiteSpace(route.toGeoName) ||
                    string.IsNullOrWhiteSpace(route.toLatitude) ||
                    string.IsNullOrWhiteSpace(route.toLongitude))
                {
                    return ApiResponse<int>.Fail("Invalid TO geofence data for dynamic routing", 400);
                }
            }
        }

        return ApiResponse<int>.Ok(0);
    }

    #endregion

    #region Helpers

    private bool IsOneTime(string frequency) =>
        frequency.Equals("ONE-TIME", StringComparison.OrdinalIgnoreCase);

    private bool IsRecurring(string frequency) =>
        frequency.Equals("RECURRING", StringComparison.OrdinalIgnoreCase);

    private bool IsValidFrequency(string frequency) =>
        IsOneTime(frequency) || IsRecurring(frequency);

    private bool IsInternalFleet(string fleetSource) =>
        fleetSource.Equals("INTERNAL", StringComparison.OrdinalIgnoreCase);

    private bool IsExternalFleet(string fleetSource) =>
        fleetSource.Equals("EXTERNAL", StringComparison.OrdinalIgnoreCase);

    private bool IsDynamicRouting(string routingModel) =>
        routingModel.Equals("DYNAMIC", StringComparison.OrdinalIgnoreCase);


    #endregion

    private async Task CreateGeofencesAndMapAsync(
    TripPlanRequestDTO request,
    IDbTransaction transaction)
    {
        int maxSequence = request.routeDetails.Max(x => x.sequence);
        foreach (var route in request.routeDetails)
        {
            //Create FROM geofence
            if (route.fromGeoId == 0)
            {
                var fromGeoRequest = new GeofenceRequestDTO
                {
                    displayName= route.fromGeoName,
                    address = route.fromGeoName,
                    latitude = route.fromLatitude,
                    longitude = route.fromLongitude,
                    accountId = request.accountId,
                    createdBy=0
                };

                int fromGeoId = await _commonApi.CreateGeofenceAsync(fromGeoRequest);

                route.fromGeoId = fromGeoId;
                if (route.sequence == 1)
                {
                    request.startGeoId = fromGeoId;
                }
            }

            //Create TO geofence
            if (route.toGeoId == 0)
            {
                var toGeoRequest = new GeofenceRequestDTO
                {
                    displayName= route.toGeoName,
                    address = route.toGeoName,
                    latitude = route.toLatitude,
                    longitude = route.toLongitude,
                    accountId = request.accountId,
                    createdBy=0
                };

                int toGeoId = await _commonApi.CreateGeofenceAsync(toGeoRequest);

                route.toGeoId = toGeoId;

                if (route.sequence == maxSequence)
                {
                    request.endGeoId = route.toGeoId;
                }
            }
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
        var validationResult = ValidateRequest(request);
        if (!validationResult.success)
            return ApiResponse<bool>.Fail(validationResult.message, 400);

        if (request.routeDetails == null || !request.routeDetails.Any())
            return ApiResponse<bool>.Fail("Route details are required.", 400);

        using var connection = _dbConnectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            //Dynamic routing
            if (IsDynamicRouting(request.routingModel))
            {
                await CreateGeofencesAndMapAsync(request, transaction);
            }

            // 1. Calculate Totals
            int totalLeadTime = request.routeDetails.Sum(x => x.leadTime);
            int totalETA = request.routeDetails.Sum(x => x.rta);

            DateTime? parsedTravelDate = null;
            if (request.frequency.Equals("one-time", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(request.travelDate))
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

            if (request.frequency.Equals("one-time", StringComparison.OrdinalIgnoreCase))
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