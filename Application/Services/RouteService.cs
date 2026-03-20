using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Application.Interfaces;
using FleetBharat.TMSService.Domain.Entities.TMS;
using Infrastructure.ExternalServices;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace FleetBharat.TMSService.Application.Services
{
    public class RouteService : IRouteService
    {
        private readonly AppDbContext _db;
        private readonly CommonApiClient _commonApi;

        public RouteService(AppDbContext db, CommonApiClient commonApi)
        {
            _db = db;
            _commonApi = commonApi;
        }

        public async Task<RouteRequestDTO> CreateOrUpdateAsync(RouteRequestDTO route)
        {
            if (route == null) throw new ArgumentNullException(nameof(route));
            if (string.IsNullOrWhiteSpace(route.routeName))
                throw new ArgumentException("routeName is required.", nameof(route));

            // Ensure all DB changes for create/update are applied in a single transaction
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                if (route.routeId > 0)
                {
                    var existing = await _db.Routes.FirstOrDefaultAsync(r => r.RouteId == route.routeId);
                    if (existing == null) throw new KeyNotFoundException("Route not found.");

                    existing.RouteName = route.routeName;
                    existing.RoutePath = route.routePath;
                    existing.RouteType = route.routeType;
                    existing.AccountId = route.accountId;
                    existing.StartGeoId = route.startGeoId;
                    existing.EndGeoId = route.endGeoId;
                    existing.TotalDistance = route.totalDistance;
                    existing.TotalTime = route.totalTime;
                    existing.IsActive = route.isActive;
                    existing.UpdatedBy = route.createdBy; 
                    existing.UpdatedDatetime = DateTime.UtcNow;

                    var existingStops = _db.RouteStops.Where(s => s.RouteId == existing.RouteId);
                    _db.RouteStops.RemoveRange(existingStops);

                    if (route.stopDetails != null && route.stopDetails.Any())
                    {
                        var stops = route.stopDetails.Select(s => new RouteStop
                        {
                            RouteId = existing.RouteId,
                            Sequence = s.sequence,
                            FromGeoId = s.fromGeoId,
                            ToGeoId = s.toGeoId,
                            Distance = s.distance,
                            Time = s.time
                        });
                        await _db.RouteStops.AddRangeAsync(stops);
                    }

                    await _db.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return await GetRouteAsync(existing.RouteId) ?? throw new InvalidOperationException("Failed to retrieve updated route.");
                }

                var entity = new RouteMaster
                {
                    RouteName = route.routeName,
                    RoutePath = route.routePath,
                    RouteType = route.routeType,
                    AccountId = route.accountId,
                    StartGeoId = route.startGeoId,
                    EndGeoId = route.endGeoId,
                    TotalDistance = route.totalDistance,
                    TotalTime = route.totalTime,
                    IsActive =route.isActive,
                    CreatedBy= route.createdBy,
                    CreatedDatetime=DateTime.UtcNow,
                };

                await _db.Routes.AddAsync(entity);
                await _db.SaveChangesAsync();

                if (route.stopDetails != null && route.stopDetails.Any())
                {
                    var stops = route.stopDetails.Select(s => new RouteStop
                    {
                        RouteId = entity.RouteId,
                        Sequence = s.sequence,
                        FromGeoId = s.fromGeoId,
                        ToGeoId = s.toGeoId,
                        Distance = s.distance,
                        Time = s.time
                    });

                    await _db.RouteStops.AddRangeAsync(stops);
                    await _db.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return await GetRouteAsync(entity.RouteId) ?? throw new InvalidOperationException("Failed to retrieve created route.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<RouteRequestDTO?> GetRouteAsync(int id)
        {
            var route = await _db.Routes.AsNoTracking().FirstOrDefaultAsync(r => r.RouteId == id);
            if (route == null) return null;

            var stops = await _db.RouteStops.AsNoTracking()
                .Where(s => s.RouteId == id)
                .OrderBy(s => s.Sequence)
                .Select(s => new StopDetailsDTO
                {
                    sequence = s.Sequence,
                    fromGeoId = s.FromGeoId,
                    toGeoId = s.ToGeoId,
                    distance = s.Distance,
                    time = s.Time
                })
                .ToListAsync();

            var result = new RouteRequestDTO
            {
                routeId = route.RouteId,
                routeName = route.RouteName,
                routePath = route.RoutePath,
                routeType = route.RouteType,
                accountId = route.AccountId,
                startGeoId = route.StartGeoId,
                endGeoId = route.EndGeoId,
                totalDistance = route.TotalDistance,
                totalTime = route.TotalTime,
                isActive= route.IsActive,
                stopDetails = stops
            };

            return result;
        }

        public async Task<RouteListUiResponseDto> GetRoutesByAccountAsync(int accountId, int page = 1, int pageSize = 20)
        {
            var query = _db.Routes.AsNoTracking().Where(r => r.AccountId == accountId);

            var total = await query.CountAsync();

            // get paged routes
            var routes = await query
                .OrderBy(r => r.RouteId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new RouteListUiResponseDto();

            response.Summary.TotalRoutes = total;
            response.Summary.TotalActiveRoutes = await _db.Routes.AsNoTracking().Where(r => r.AccountId == accountId && r.IsActive == true).CountAsync();
            response.Summary.TotalInactiveRoutes = total - response.Summary.TotalActiveRoutes;

            if (routes == null || routes.Count == 0)
            {
                // set empty paged result
                response.Assignments = new PagedResultDto<RouteResponseDTO>
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = total,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                    Items = new List<RouteResponseDTO>()
                };

                return response;
            }

            var routeIds = routes.Select(r => r.RouteId).ToList();

            var stops = await _db.RouteStops.AsNoTracking()
                .Where(s => routeIds.Contains(s.RouteId))
                .OrderBy(s => s.Sequence)
                .ToListAsync();

            // Fetch geofences and accounts once
            var geofences = await _commonApi.GetGeofencesAsync(accountId, 100) ?? new List<CommonResponseDTO>();
            var accounts = await _commonApi.GetAccountsAsync(200) ?? new List<CommonResponseDTO>();

            var stopsByRoute = stops.GroupBy(s => s.RouteId)
                .ToDictionary(g => g.Key, g => g.Select(s => new StopDetailsDTO
                {
                    sequence = s.Sequence,
                    fromGeoId = s.FromGeoId,
                    toGeoId = s.ToGeoId,
                    distance = s.Distance,
                    time = s.Time
                }).ToList());

            var items = routes.Select(route => new RouteResponseDTO
            {
                routeId = route.RouteId,
                routeName = route.RouteName,
                routePath = route.RoutePath,
                routeType = route.RouteType,
                accountId = route.AccountId,
                startGeoId = route.StartGeoId,
                endGeoId = route.EndGeoId,
                startGeoName = geofences.FirstOrDefault(g => g.id == route.StartGeoId)?.value,
                endGeoName = geofences.FirstOrDefault(g => g.id == route.EndGeoId)?.value,
                accountName = accounts.FirstOrDefault(a => a.id == route.AccountId)?.value,
                totalDistance = route.TotalDistance,
                totalTime = route.TotalTime,
                stopDetails = stopsByRoute.ContainsKey(route.RouteId) ? stopsByRoute[route.RouteId] : new List<StopDetailsDTO>(),
                stopCount= stopsByRoute.ContainsKey(route.RouteId) ? (stopsByRoute[route.RouteId].Count-1): 0,
                createdBy = route.CreatedBy,
                isActive = route.IsActive
            }).ToList();

            response.Assignments = new PagedResultDto<RouteResponseDTO>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Items = items
            };

            return response;
        }

        public async Task<ApiResponse<List<DropdownDto>>> GetRouteDropdown(int accountId)
        {
            var routes = await _db.Routes.AsNoTracking()
                .Where(x => x.AccountId == accountId && x.IsActive)
                .OrderBy(x => x.RouteName)
                .Select(x => new DropdownDto
                {
                    Id = x.RouteId,
                    Value = x.RouteName ?? ""
                })
                .ToListAsync();

            if (routes == null || routes.Count == 0)
            {
                return ApiResponse<List<DropdownDto>>.Fail("No routes found for this account", 404);
            }

            return ApiResponse<List<DropdownDto>>.Ok(routes, "Routes fetched successfully");
        }
    }
}
