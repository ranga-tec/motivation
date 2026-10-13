using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Poms.Web.Api;

public sealed class FlexibleTimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    private static readonly string[] AcceptedFormats = ["HH:mm", "HH:mm:ss", "HH:mm:ss.FFFFFFF"];

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value is not null && TimeOnly.TryParseExact(
                value,
                AcceptedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var time))
        {
            return time;
        }

        throw new JsonException("Time must use HH:mm or HH:mm:ss format.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
}
