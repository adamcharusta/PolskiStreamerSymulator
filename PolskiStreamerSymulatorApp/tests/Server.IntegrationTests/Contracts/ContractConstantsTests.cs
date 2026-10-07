using PolskiStreamerSymulatorApp.Contracts.Localization;
using PolskiStreamerSymulatorApp.Contracts.Problems;

namespace PolskiStreamerSymulatorApp.Server.IntegrationTests.Contracts;

public sealed class ContractConstantsTests
{
    [Theory]
    [InlineData("pl-PL", true)]
    [InlineData("en", true)]
    [InlineData("pl-pl", false)]
    [InlineData("en-US", false)]
    [InlineData("de", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void OnlyTheShippedLocalesAreSupported(string? locale, bool supported)
    {
        Assert.Equal(supported, SupportedLocales.IsSupported(locale));
    }

    [Fact]
    public void PolishIsTheDefaultLocale()
    {
        Assert.Equal(SupportedLocales.Polish, SupportedLocales.Default);
        Assert.Equal(new[] { "pl-PL", "en" }, SupportedLocales.All);
    }

    [Fact]
    public void ProblemCodesAreDistinctSnakeCaseValues()
    {
        Assert.Equal(ProblemCodes.All.Count, ProblemCodes.All.Distinct(StringComparer.Ordinal).Count());
        Assert.All(ProblemCodes.All, static code => Assert.Matches("^[a-z]+(_[a-z]+)*$", code));
    }
}
