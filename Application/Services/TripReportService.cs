using System.Globalization;
using System.Text.Json;
using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Application.Interfaces;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;
using Infrastructure.ExternalServices;

namespace FleetBharat.TMSService.Application.Services
{
    public class TripReportService : ITripReportService
    {
        private static readonly string[] AcceptedDateFormats =
        [
            "dd/MM/yyyy HH:mm",
            "dd/MM/yyyy H:mm",
            "dd/MM/yyyy",
            "dd-MM-yyyy HH:mm:ss",
            "dd-MM-yyyy HH:mm",
            "dd-MM-yyyy",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd"
        ];

        private readonly ITripReportRepository _tripReportRepository;
        private readonly CommonApiClient _commonApi;

        public TripReportService(ITripReportRepository tripReportRepository, CommonApiClient commonApi)
        {
            _tripReportRepository = tripReportRepository;
            _commonApi = commonApi;
        }

        public async Task<ApiResponse<TripReportListUiResponseDto>> GetTripReportAsync(TripReportFilterDto request)
        {
            if (request.accountId <= 0)
            {
                return ApiResponse<TripReportListUiResponseDto>.Fail("accountId is required.", 400);
            }

            if (!TryParseOptionalDate(request.fromDate, out var fromDate, out var fromError))
            {
                return ApiResponse<TripReportListUiResponseDto>.Fail(fromError!, 400);
            }

            if (!TryParseOptionalDate(request.toDate, out var toDate, out var toError))
            {
                return ApiResponse<TripReportListUiResponseDto>.Fail(toError!, 400);
            }

            if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            {
                return ApiResponse<TripReportListUiResponseDto>.Fail("fromDate cannot be greater than toDate.", 400);
            }

            var repositoryRequest = new TripReportRepositoryRequestDto
            {
                accountId = request.accountId,
                vehicleNo = NormalizeText(request.vehicleNo),
                tripStatus = NormalizeTripStatus(request.tripStatus),
                deviceType = NormalizeDeviceType(request.deviceType),
                tripType = NormalizeTripDirection(request.tripType),
                fromDate = fromDate,
                toDate = toDate
            };

            var (summary, rawItems, totalRecords) = await _tripReportRepository.GetTripReportAsync(repositoryRequest);

            var geofenceTask = _commonApi.GetGeofencesAsync(request.accountId, 500);
            var accountTask = _commonApi.GetAccountsAsync(500);

            await Task.WhenAll(geofenceTask, accountTask);

            var geofences = geofenceTask.Result ?? new List<CommonResponseDTO>();
            var accounts = accountTask.Result ?? new List<CommonResponseDTO>();

            var geofenceLookup = geofences
                .GroupBy(x => x.id)
                .ToDictionary(x => x.Key, x => x.First().value ?? string.Empty);

            var accountLookup = accounts
                .GroupBy(x => x.id)
                .ToDictionary(x => x.Key, x => x.First().value ?? string.Empty);

            var items = rawItems.Select(item => new TripReportItemDto
            {
                tripId = item.tripId,
                tripNo = item.tripNo ?? $"TRIP-{item.tripId}",
                accountId = item.accountId,
                organization = ResolveAccountName(accountLookup, item.accountId),
                driverId = item.driverId,
                driverName = item.driverName ?? string.Empty,
                vehicleId = item.vehicleId,
                vehicleNo = item.vehicleNo ?? string.Empty,
                deviceType = item.deviceType ?? string.Empty,
                deviceNumber = item.deviceNumber,
                lockStatus = string.Empty,
                startGeoId = item.startGeoId,
                origin = ResolveGeofenceName(geofenceLookup, item.startGeoId, item.geofenceJson, "START"),
                endGeoId = item.endGeoId,
                destination = ResolveGeofenceName(geofenceLookup, item.endGeoId, item.geofenceJson, "END"),
                type = string.IsNullOrWhiteSpace(item.tripDirection) ? "Forward" : item.tripDirection,
                status = item.status ?? string.Empty,
                etd = item.etd,
                rta = item.rta,
                startTime = FormatDisplayDate(item.etd),
                eta = FormatDisplayDate(item.rta),
                isCurrentTrip = item.isCurrentTrip,
                tripCompleted = item.tripCompleted,
                legendStatus = item.legendStatus,
                legendIcon = item.legendIcon,
                segmentCount = item.segmentCount,
                totalDistance = item.totalDistance
            }).ToList();

            var response = new TripReportListUiResponseDto
            {
                summary = summary,
                totalRecords = totalRecords,
                data = items
            };

            return ApiResponse<TripReportListUiResponseDto>.Ok(response, "Trip report data retrieved successfully.");
        }

        private static string? NormalizeText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? NormalizeTripStatus(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim().ToLowerInvariant() switch
            {
                "all" => null,
                "all status" => null,
                "completed" => "Completed",
                "delayed" => "Delayed",
                "in transit" => "In Transit",
                "planned" => "Planned",
                "ready" => "Ready",
                _ => value.Trim()
            };
        }

        private static string? NormalizeDeviceType(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim().ToLowerInvariant() switch
            {
                "all" => null,
                "all type" => null,
                "all types" => null,
                "e-lock" => "E-Lock",
                "elock" => "E-Lock",
                "gps" => "GPS",
                "logger" => "Logger",
                _ => value.Trim()
            };
        }

        private static string? NormalizeTripDirection(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim().ToLowerInvariant() switch
            {
                "all" => null,
                "all type" => null,
                "all types" => null,
                "forward" => "Forward",
                "reverse" => "Reverse",
                _ => value.Trim()
            };
        }

        private static bool TryParseOptionalDate(string? value, out DateTime? parsedDate, out string? error)
        {
            parsedDate = null;
            error = null;

            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            var trimmed = value.Trim();
            if (DateTime.TryParseExact(trimmed, AcceptedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            {
                parsedDate = exact;
                return true;
            }

            if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var flexible))
            {
                parsedDate = flexible;
                return true;
            }

            error = "Invalid date format. Use dd/MM/yyyy HH:mm.";
            return false;
        }

        private static string ResolveAccountName(IReadOnlyDictionary<int, string> accountLookup, int accountId)
        {
            return accountLookup.TryGetValue(accountId, out var name) ? name : string.Empty;
        }

        private static string ResolveGeofenceName(IReadOnlyDictionary<int, string> geofenceLookup, int geofenceId, string? geofenceJson, string pointType)
        {
            if (geofenceLookup.TryGetValue(geofenceId, out var name) && !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            return TryResolveGeofenceNameFromJson(geofenceJson, geofenceId, pointType) ?? string.Empty;
        }

        private static string? TryResolveGeofenceNameFromJson(string? geofenceJson, int geofenceId, string pointType)
        {
            if (string.IsNullOrWhiteSpace(geofenceJson))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(geofenceJson);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                foreach (var point in document.RootElement.EnumerateArray())
                {
                    var currentGeoId = ReadInt(point, "geofenceId");
                    var currentPointType = ReadString(point, "pointType");

                    if ((currentGeoId.HasValue && currentGeoId.Value == geofenceId) ||
                        string.Equals(currentPointType, pointType, StringComparison.OrdinalIgnoreCase))
                    {
                        var address = ReadString(point, "geofenceAddress");
                        if (!string.IsNullOrWhiteSpace(address))
                        {
                            return address;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                return null;
            }

            return null;
        }

        private static int? ReadInt(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                return null;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var numericValue))
            {
                return numericValue;
            }

            if (property.ValueKind == JsonValueKind.String &&
                int.TryParse(property.GetString(), out var stringValue))
            {
                return stringValue;
            }

            return null;
        }

        private static string? ReadString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                return null;
            }

            return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
        }

        private static string FormatDisplayDate(DateTime? value)
        {
            return value.HasValue
                ? value.Value
                    .ToString("dd MMM, hh:mm tt", CultureInfo.InvariantCulture)
                    .Replace("AM", "am", StringComparison.Ordinal)
                    .Replace("PM", "pm", StringComparison.Ordinal)
                : string.Empty;
        }
    }
}
