using System.Text.Json;
using FluentAssertions;
using Poms.Web.Api;

namespace Poms.Tests;

public sealed class FlexibleTimeOnlyJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new FlexibleTimeOnlyJsonConverter() }
    };

    [Theory]
    [InlineData("\"10:30\"")]
    [InlineData("\"10:30:00\"")]
    public void Deserialize_AcceptsBrowserAndApiTimeFormats(string json)
    {
        JsonSerializer.Deserialize<TimeOnly>(json, Options).Should().Be(new TimeOnly(10, 30));
    }

    [Fact]
    public void Serialize_UsesCanonicalTimeFormat()
    {
        JsonSerializer.Serialize(new TimeOnly(10, 30), Options).Should().Be("\"10:30:00\"");
    }
}
