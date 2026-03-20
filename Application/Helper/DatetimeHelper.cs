using System.Globalization;

namespace FleetBharat.TMSService.Application.Helper
{
    public static class DatetimeHelper
    {
        private static readonly string[] SupportedInputFormats =
        {
        "dd/MM/yyyy",
        "d/M/yyyy",
        "MM/dd/yyyy",
        "M/d/yyyy",
        "yyyy-MM-dd",
        "yyyy/MM/dd"
    };

        public static DateTime? ParseToDate(string inputDate, string[] inputFormats = null, CultureInfo culture = null)
        {
            if (string.IsNullOrWhiteSpace(inputDate))
                return null;

            inputFormats ??= SupportedInputFormats;
            culture ??= CultureInfo.InvariantCulture;

            return DateTime.TryParseExact(
                inputDate,
                inputFormats,
                culture,
                DateTimeStyles.None,
                out var parsedDate)
                ? DateTime.SpecifyKind(parsedDate, DateTimeKind.Unspecified)
                : null;
        }

        public static string ParseAndFormat(string inputDate, string outputFormat, string[] inputFormats = null, CultureInfo culture = null)
        {
            var date = ParseToDate(inputDate, inputFormats, culture);

            return date?.ToString(outputFormat, culture);
        }
    }
}
