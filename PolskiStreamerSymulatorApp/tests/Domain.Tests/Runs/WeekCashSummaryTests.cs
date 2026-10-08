using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Runs;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Runs;

public sealed class WeekCashSummaryTests
{
    [Fact]
    public void BalanceIllustrationReconcilesToOneThousandSixHundredNinetyThree()
    {
        // The ledger example from balance.md: 1,500 + 200 + 35 + 18 - 60 = 1,693 PLN.
        RunState state = RunStateFixtures.Active() with
        {
            MoneyPln = 1_693,
            Viewers = 25,
            Ledger =
            [
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionCost, 0, CashFlowCategory.Expenses, 60),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 0, CashFlowCategory.Sponsors, 200),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 1, CashFlowCategory.Donations, 35),
                RunStateFixtures.Subscription(1, 18),
            ],
            History =
            [
                RunStateFixtures.QuietFirstWeek() with
                {
                    Action = new ActionResolution("sponsor_pitch", Roll: 100, OutcomeId: "deal", ViewersDelta: 5, DramaDelta: 0),
                },
            ],
        };

        WeekCashSummary summary = WeekCashSummary.For(Validate(state), week: 1);

        Assert.Equal(new WeekCashSummary(1, 1_500, 200, 35, 18, 60, 1_693), summary);
    }

    [Fact]
    public void PendingWeekCoversTheWeekSoFar()
    {
        WeekCashSummary summary = WeekCashSummary.For(Validate(RunStateFixtures.PendingWeek()), week: 2);

        Assert.Equal(new WeekCashSummary(2, 1_502, 250, 0, 3, 80, 1_675), summary);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void WeekWithoutARecordIsRejected(int week)
    {
        ValidatedRunState run = Validate(RunStateFixtures.Active());

        Assert.Throws<ArgumentOutOfRangeException>(() => WeekCashSummary.For(run, week));
    }

    [Fact]
    public void TotalsBeyondSixtyFourBitsThrowInsteadOfWrapping()
    {
        // An edited save may hold huge entries that still reconcile; their week total does not fit in 64 bits.
        RunState state = RunStateFixtures.Active() with
        {
            Ledger =
            [
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 0, CashFlowCategory.Sponsors, long.MaxValue),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 1, CashFlowCategory.Sponsors, long.MaxValue),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 2, CashFlowCategory.Expenses, long.MaxValue),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 3, CashFlowCategory.Expenses, long.MaxValue),
                RunStateFixtures.Subscription(1, 2),
            ],
        };
        ValidatedRunState run = Validate(state);

        Assert.Throws<OverflowException>(() => WeekCashSummary.For(run, week: 1));
    }

    private static ValidatedRunState Validate(RunState state)
    {
        RunStateValidation validation = RunStateValidator.Validate(state, RunStateFixtures.Parameters);
        Assert.True(validation.IsValid, string.Join(", ", validation.Errors));
        return validation.Value;
    }
}
