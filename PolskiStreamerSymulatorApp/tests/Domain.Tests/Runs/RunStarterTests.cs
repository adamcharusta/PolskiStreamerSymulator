using System.Text;
using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Randomness;
using PolskiStreamerSymulatorApp.Domain.Runs;
using PolskiStreamerSymulatorApp.Domain.Tests.Catalog;
using PolskiStreamerSymulatorApp.Domain.Versioning;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Runs;

public sealed class RunStarterTests
{
    [Fact]
    public void StartsWeekOneFromThePinnedParameters()
    {
        RunStateValidation start = RunStarter.Start(DraftCatalog.Validated(), RunStateFixtures.RunId, "NeonBorsuk", seed: 42, stream: 54);

        Assert.True(start.IsValid, string.Join(", ", start.Errors));
        RunState state = start.Value.State;
        Assert.Equal(RulesVersion.Current, state.RulesVersion);
        Assert.Equal(1, state.CatalogVersion);
        Assert.Equal(RunStateFixtures.RunId, state.RunId);
        Assert.Equal("NeonBorsuk", state.StreamerName);
        Assert.Equal(1, state.Week);
        Assert.Equal(RunStatus.Active, state.Status);
        Assert.Equal(1_500, state.MoneyPln);
        Assert.Equal(20, state.Viewers);
        Assert.Equal(50, state.Drama);
        Assert.Equal(Pcg32.Seed(42, 54), state.Rng);
        Assert.Empty(state.Ledger);
        Assert.Empty(state.History);
        Assert.Null(state.CurrentWeek);
        Assert.Empty(state.Flags);
        Assert.Null(state.Ending);
        Assert.Equal(RunStateFixtures.Parameters, start.Value.Parameters);
    }

    [Fact]
    public void UsesTheCatalogueParametersRatherThanFirstPublishedValues()
    {
        GameParameters parameters = RunStateFixtures.Parameters with { StartingMoneyPln = 900, StartingViewers = 0, StartingDrama = 70 };

        RunStateValidation start = RunStarter.Start(DraftCatalog.Validated(parameters), RunStateFixtures.RunId, "NeonBorsuk", 1, 1);

        Assert.True(start.IsValid);
        Assert.Equal((900L, 0, 70), (start.Value.State.MoneyPln, start.Value.State.Viewers, start.Value.State.Drama));
    }

    [Fact]
    public void StoresTheNormalizedName()
    {
        string decomposed = "Zażółć".Normalize(NormalizationForm.FormD);

        RunStateValidation start = RunStarter.Start(DraftCatalog.Validated(), RunStateFixtures.RunId, $"  {decomposed} ", 1, 1);

        Assert.True(start.IsValid);
        Assert.Equal("Zażółć".Normalize(NormalizationForm.FormC), start.Value.State.StreamerName);
    }

    [Fact]
    public void InvalidNameIsReportedWithTheRunStateCode()
    {
        RunStateValidation start = RunStarter.Start(DraftCatalog.Validated(), RunStateFixtures.RunId, "A", 1, 1);

        Assert.False(start.IsValid);
        Assert.Equal([new RunStateError(RunStateErrorCodes.InvalidStreamerName, "streamerName")], start.Errors);
    }

    [Fact]
    public void EmptyRunIdIsReportedWithTheRunStateCode()
    {
        RunStateValidation start = RunStarter.Start(DraftCatalog.Validated(), Guid.Empty, "NeonBorsuk", 1, 1);

        Assert.False(start.IsValid);
        Assert.Equal([new RunStateError(RunStateErrorCodes.InvalidRunId, "runId")], start.Errors);
    }
}
