using Newtonsoft.Json;
using System.Globalization;

namespace FleetBharat.TMSService.Application.Filters
{
    public class SafeDecimalConverter : JsonConverter<decimal>
    {
        public override decimal ReadJson(JsonReader reader, Type objectType, decimal existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Integer || reader.TokenType == JsonToken.Float)
                return Convert.ToDecimal(reader.Value);

            if (reader.TokenType == JsonToken.String)
            {
                var str = reader.Value?.ToString();

                if (string.IsNullOrWhiteSpace(str))
                    return 0;

                if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                    return val;
            }

            return 0; // fallback
        }

        public override void WriteJson(JsonWriter writer, decimal value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }
    }
}
