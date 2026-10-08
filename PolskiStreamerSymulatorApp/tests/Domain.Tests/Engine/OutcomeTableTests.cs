using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Engine;
using PolskiStreamerSymulatorApp.Domain.Tests.Catalog;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Engine;

public sealed class OutcomeTableTests
{
    private static IReadOnlyList<WeeklyActionOutcome> RegularStream => DraftCatalog.WeeklyActions[0].Outcomes;

    [Theory]
    [InlineData(0, "steady")]
    [InlineData(5_999, "steady")]
    [InlineData(6_000, "good_chat")]
    [InlineData(8_999, "good_chat")]
    [InlineData(9_000, "slow_evening")]
    [InlineData(9_999, "slow_evening")]
    public void RollSelectsTheOutcomeWhoseIntervalContainsIt(int roll, string expectedOutcomeId)
    {
        WeeklyActionOutcome outcome = OutcomeTable.Select(RegularStream, roll);

        Assert.Equal(expectedOutcomeId, outcome.OutcomeId);
    }

    [Fact]
    public void ZeroChanceOutcomeIsNeverSelected()
    {
        WeeklyActionOutcome[] outcomes =
        [
            DraftCatalog.Outcome("never_first", 0, viewersDelta: 0, dramaDelta: 0),
            DraftCatalog.Outcome("always", 10_000, viewersDelta: 0, dramaDelta: 0),
            DraftCatalog.Outcome("never_last", 0, viewersDelta: 0, dramaDelta: 0),
        ];

        string[] selected = [.. Enumerable.Range(0, 10_000).Select(roll => OutcomeTable.Select(outcomes, roll).OutcomeId).Distinct()];

        Assert.Equal(["always"], selected);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10_000)]
    public void RollOutsideZeroToNineThousandNineHundredNinetyNineIsRejected(int roll)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OutcomeTable.Select(RegularStream, roll));
    }

    [Fact]
    public void TableThatDoesNotSumToTenThousandIsRejected()
    {
        WeeklyActionOutcome[] outcomes = [DraftCatalog.Outcome("partial", 9_999, viewersDelta: 0, dramaDelta: 0)];

        Assert.Throws<ArgumentException>(() => OutcomeTable.Select(outcomes, 0));
    }
}
