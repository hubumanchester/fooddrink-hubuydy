using System.Text.Json.Serialization;
using System.Text.Json;

namespace FoodVisionMauiDemo.Models
{
    public class AmapAroundSearchResponse
    {
        [JsonPropertyName("status")]
        [JsonConverter(typeof(AmapStringConverter))]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("info")]
        [JsonConverter(typeof(AmapStringConverter))]
        public string Info { get; set; } = string.Empty;

        [JsonPropertyName("infocode")]
        [JsonConverter(typeof(AmapStringConverter))]
        public string InfoCode { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        [JsonConverter(typeof(AmapStringConverter))]
        public string Count { get; set; } = "0";

        [JsonPropertyName("pois")]
        public List<AmapPoi> Pois { get; set; } = new();
    }

    public class AmapPoi
    {
        [JsonPropertyName("name")]
        [JsonConverter(typeof(AmapStringConverter))]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        [JsonConverter(typeof(AmapStringConverter))]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        [JsonConverter(typeof(AmapStringConverter))]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("location")]
        [JsonConverter(typeof(AmapStringConverter))]
        public string Location { get; set; } = string.Empty;

        [JsonPropertyName("distance")]
        [JsonConverter(typeof(AmapStringConverter))]
        public string Distance { get; set; } = string.Empty;

        [JsonPropertyName("tel")]
        [JsonConverter(typeof(AmapStringConverter))]
        public string Tel { get; set; } = string.Empty;
    }

    public class AmapStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString() ?? string.Empty,
                JsonTokenType.Number => reader.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture),
                JsonTokenType.StartArray => ReadArray(ref reader),
                JsonTokenType.Null => string.Empty,
                _ => string.Empty
            };
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        private static string ReadArray(ref Utf8JsonReader reader)
        {
            var values = new List<string>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                if (reader.TokenType == JsonTokenType.String)
                    values.Add(reader.GetString() ?? string.Empty);
                else if (reader.TokenType == JsonTokenType.Number)
                    values.Add(reader.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            return string.Join(", ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
        }
    }
}
