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


        public static bool TryParse(string frequency, string value, out PlannedTime result)
        {
            result = new PlannedTime { Raw = value };

            if (string.IsNullOrWhiteSpace(value))
                return true;

            if (frequency == "RECURRING")
            {
                if (DateTime.TryParseExact(
                value,
                "hh:mm tt",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var time))
                {
                    result.Time = time.TimeOfDay; // ✅ store as TimeSpan
                    return true;
                }

                return false;
            }

            // ONE-TIME
            if (DateTime.TryParseExact(value,
                "dd/MM/yyyy hh:mm tt",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var dt))
            {
                result.DateTime = dt;
                return true;
            }

            return false;
        }
    }

    public class PlannedTime
    {
        public string Raw { get; set; }

        public TimeSpan? Time { get; set; }        // For RECURRING
        public DateTime? DateTime { get; set; }    // For ONE-TIME
    }
}
