using Funcy.Infrastructure.Azure;
using Xunit;

namespace Funcy.Tests.Azure;

public class AppInsightsConnectionStringTests
{
    [Fact]
    public void ParseInstrumentationKey_FromFullConnectionString()
    {
        const string connectionString =
            "InstrumentationKey=00000000-1111-2222-3333-444444444444;" +
            "IngestionEndpoint=https://westeurope.in.applicationinsights.azure.com/";

        var key = AppInsightsConnectionString.ParseInstrumentationKey(connectionString);

        Assert.Equal("00000000-1111-2222-3333-444444444444", key);
    }

    [Fact]
    public void ParseInstrumentationKey_IsCaseInsensitiveOnKeyName()
    {
        var key = AppInsightsConnectionString.ParseInstrumentationKey("instrumentationkey=abc;X=y");

        Assert.Equal("abc", key);
    }

    [Fact]
    public void ParseInstrumentationKey_WhenKeyMissing_ReturnsNull()
    {
        Assert.Null(AppInsightsConnectionString.ParseInstrumentationKey("IngestionEndpoint=https://x/"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseInstrumentationKey_WhenEmpty_ReturnsNull(string? input)
    {
        Assert.Null(AppInsightsConnectionString.ParseInstrumentationKey(input));
    }
}
