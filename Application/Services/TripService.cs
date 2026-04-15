using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Application.Helper;
using FleetBharat.TMSService.Domain.Entities.TMS;
using FleetBharat.TMSService.Infrastructure.ConnectionFactory;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;
using Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;
using System.Text.Json;

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

            //int totalLeadTime = request.routeDetails.Sum(x => x.leadTime);
            int totalETA = request.routeDetails.Sum(x => x.googleSuggestedTime);

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

            // Convert Points → Segments
            var segments = ConvertToRouteDetails(request);

            var ordered = request.routeDetails
                .OrderBy(x => x.sequence)
                .ToList();
            // Set Start & End Geo
            request.startGeoId = ordered
                .First().geofenceId;

            request.endGeoId = ordered.Last().geofenceId;

            var plannedEntryTime = ordered.First().plannedExitTime;  // ETD
            var plannedExitTime = ordered.Last().plannedEntryTime;  // final arrival

            //Serialize Secondary Devices (JSONB)
            var secondaryDevicesJson = request.secondaryDevice?.Any() == true
                ? JsonSerializer.Serialize(request.secondaryDevice)
                : "[]";
            

            int planId = await _tripPlanRepository.CreateTripPlanAsync(
                request,
                parsedTravelDate,
                totalETA,
                secondaryDevicesJson,
                plannedEntryTime,
                plannedExitTime,
                transaction);

            await _tripPlanRepository.InsertRouteDetailsAsync(
                planId,
                segments,
                transaction);

            await _tripPlanRepository.InsertGeofencePointsAsync(planId, request.routeDetails, transaction);

            if (IsOneTime(request.frequency))
            {

                DateTime baseDate = DatetimeHelper.ParseToDate(request.travelDate, ["dd/MM/yyyy"]) ?? DateTime.Today;

                DateTime TripETD = ParsePlannedTime(
                ordered.First().plannedExitTime,
                IsOneTime(request.frequency),
                baseDate);

                DateTime TripRTA = ParsePlannedTime(
                ordered.Last().plannedEntryTime,
                IsOneTime(request.frequency),
                baseDate);

                await _tripPlanRepository.CreateTransAndDetTripAsync(planId, request, segments, TripETD, TripRTA, secondaryDevicesJson, transaction);
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
                if (string.IsNullOrWhiteSpace(route.geofenceAddress) ||
                    string.IsNullOrWhiteSpace(route.geofenceCenterLatitude) ||
                    string.IsNullOrWhiteSpace(route.geofenceCenterLongitude))
                {
                    return ApiResponse<int>.Fail("Invalid geofence data for dynamic routing", 400);
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
            //Create geofence
            if (route.geofenceId == 0)
            {
                var fromGeoRequest = new GeofenceRequestDTO
                {
                    displayName= route.geofenceAddress,
                    address = route.geofenceAddress,
                    latitude = route.geofenceCenterLatitude,
                    longitude = route.geofenceCenterLongitude,
                    accountId = request.accountId,
                    createdBy=0
                };

                int fromGeoId = await _commonApi.CreateGeofenceAsync(fromGeoRequest);

                if (route.sequence == 1)
                {
                    request.startGeoId = fromGeoId;
                }

                if (route.sequence == maxSequence)
                {
                    request.endGeoId = fromGeoId;
                }
            }

            
        }
    }

    private List<TripPlanRouteDetailsDTO> ConvertToRouteDetails(TripPlanRequestDTO request)
    {
        var ordered = request.routeDetails
            .OrderBy(x => x.sequence)
            .ToList();

        if (!ordered.Any())
            throw new Exception("Route details cannot be empty");

        var result = new List<TripPlanRouteDetailsDTO>();

        bool isOneTime = IsOneTime(request.frequency);

        DateTime baseDate = DatetimeHelper.ParseToDate(request.travelDate, ["dd/MM/yyyy"]) ?? DateTime.Today;

        // ✅ FIX: Base time = first stop exit time (ETD)
        DateTime baseTime = ParsePlannedTime(
            ordered.First().plannedExitTime,
            isOneTime,
            baseDate);

        int cumulativeGoogleTime = 0;

        for (int i = 0; i < ordered.Count - 1; i++)
        {
            var current = ordered[i];
            var next = ordered[i + 1];

            int leadTime = 0;
            int segmentRta = 0;

            if (isOneTime)
            {
                var currentEntry = ParsePlannedTime(current.plannedEntryTime, true, baseDate);
                var currentExit = ParsePlannedTime(current.plannedExitTime, true, baseDate);
                var nextEntry = ParsePlannedTime(next.plannedEntryTime, true, baseDate);

                if (currentExit < currentEntry)
                    currentExit = currentExit.AddDays(1);

                if (nextEntry < currentExit)
                    nextEntry = nextEntry.AddDays(1);

                leadTime = (int)(currentExit - currentEntry).TotalMinutes;
                if (leadTime < 0) leadTime = 0;

                segmentRta = (int)(nextEntry - currentExit).TotalMinutes;
                if (segmentRta < 0) segmentRta = 0;
            }
            else
            {
                // ✅ RECURRING → use google suggested time
                leadTime = 0;

                segmentRta = next.googleSuggestedTime;
            }

            result.Add(new TripPlanRouteDetailsDTO
            {
                fromGeoId = current.geofenceId,
                toGeoId = next.geofenceId,
                sequence = i + 1,

                distance = next.distance ?? "0",

                leadTime = leadTime,
                rta = segmentRta,

                fromExitTime = current.plannedExitTime,
                toEntryTime = next.plannedEntryTime,

                googleSuggestedTime = next.googleSuggestedTime
            });
        }

        return result;
    }

    private DateTime ParsePlannedTime(
    string? timeValue,
    bool isOneTime,
    DateTime baseDate)
    {
        if (string.IsNullOrWhiteSpace(timeValue))
            return DateTime.MinValue;

        // ✅ ONE-TIME → Full DateTime expected
        if (isOneTime)
        {
            // Try normal parsing first
            if (DateTime.TryParse(timeValue, out var dateTime))
                return dateTime;

            // Try custom formats if needed
            string[] formats =
            {
            "dd/MM/yyyy HH:mm",
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy hh:mm tt",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss"
        };

            if (DateTime.TryParseExact(
                    timeValue,
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDateTime))
            {
                return parsedDateTime;
            }
        }
        else
        {
            // ✅ RECURRING → Only Time expected
            if (TimeSpan.TryParse(timeValue, out var time))
            {
                return baseDate.Date.Add(time);
            }

            // Optional: handle HH:mm:ss manually
            if (DateTime.TryParseExact(
                    timeValue,
                    new[] { "HH:mm", "HH:mm:ss","hh:mm tt","hh:mm:ss tt" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedTime))
            {
                return baseDate.Date.Add(parsedTime.TimeOfDay);
            }
        }

        // ❌ Fallback
        return DateTime.MinValue;
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
            plannedStartTime = trip.planned_start_time,
            googleSuggestedTime = trip.google_suggested_time ?? 0,
            plannedEndTime = trip.planned_end_time,
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
            // ✅ Dynamic routing
            if (IsDynamicRouting(request.routingModel))
            {
                await CreateGeofencesAndMapAsync(request, transaction);
            }

            // ✅ Calculate ETA (same as CREATE)
            int totalETA = request.routeDetails.Sum(x => x.googleSuggestedTime);

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

            // ✅ Convert points → segments
            var segments = ConvertToRouteDetails(request);

            var ordered = request.routeDetails
                .OrderBy(x => x.sequence)
                .ToList();

            // ✅ Set Start & End Geo
            request.startGeoId = ordered.First().geofenceId;
            request.endGeoId = ordered.Last().geofenceId;

            var plannedEntryTime = ordered.First().plannedExitTime;   // ETD
            var plannedExitTime = ordered.Last().plannedEntryTime;    // final arrival

            // ✅ Serialize secondary devices
            var secondaryDevicesJson = request.secondaryDevice?.Any() == true
                ? JsonSerializer.Serialize(request.secondaryDevice)
                : "[]";

            // ✅ Update Main Plan
            bool updated = await _tripPlanRepository.UpdateTripPlanAsync(
                request.planId,
                request,
                parsedTravelDate,
                totalETA,
                secondaryDevicesJson,
                plannedEntryTime,
                plannedExitTime,
                transaction);

            if (!updated)
                throw new KeyNotFoundException("Trip Plan not found.");

            // ✅ Delete old data
            await _tripPlanRepository.DeleteRouteDetailsByPlanIdAsync(request.planId, transaction);
            await _tripPlanRepository.DeleteGeofenceDetailsByPlanIdAsync(request.planId, transaction);

            // ✅ Insert fresh data
            await _tripPlanRepository.InsertRouteDetailsAsync(
                request.planId,
                segments,
                transaction);

            await _tripPlanRepository.InsertGeofencePointsAsync(
                request.planId,
                request.routeDetails,
                transaction);

            // ✅ Handle ONE-TIME trip (same as CREATE)
            if (IsOneTime(request.frequency))
            {
                DateTime baseDate = DatetimeHelper.ParseToDate(request.travelDate, ["dd/MM/yyyy"]) ?? DateTime.Today;

                DateTime TripETD = ParsePlannedTime(
                    ordered.First().plannedExitTime,
                    true,
                    baseDate);

                DateTime TripRTA = ParsePlannedTime(
                    ordered.Last().plannedEntryTime,
                    true,
                    baseDate);

                // ⚠️ Important: delete old trans trip before re-creating
                //await _tripPlanRepository.DeleteTransTripByPlanIdAsync(request.planId, transaction);

                await _tripPlanRepository.CreateTransAndDetTripAsync(
                    request.planId,
                    request,
                    segments,
                    TripETD,
                    TripRTA,
                    secondaryDevicesJson,
                    transaction);
            }

            transaction.Commit();
            return ApiResponse<bool>.Ok(true, "Trip plan updated successfully");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return ApiResponse<bool>.Fail($"Error updating trip plan: {ex.Message}", 500);
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