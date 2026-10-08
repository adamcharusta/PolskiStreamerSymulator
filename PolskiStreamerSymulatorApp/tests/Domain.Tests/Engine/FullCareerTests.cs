using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Engine;
using PolskiStreamerSymulatorApp.Domain.Runs;
using PolskiStreamerSymulatorApp.Domain.Tests.Catalog;
using PolskiStreamerSymulatorApp.Domain.Tests.Runs;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Engine;

public sealed class FullCareerTests
{
    private static readonly string[] Rotation = ["regular_stream", "sponsor_pitch", "provocative_stunt", "quiet_week"];

    [Fact]
    public void FiftyTwoWeekCareerStaysValidAndReconcilesEveryWeek()
    {
        ValidatedCatalog catalog = DraftCatalog.Validated();
        RunStateValidation start = RunStarter.Start(catalog, RunStateFixtures.RunId, "NeonBorsuk", seed: 2_026, stream: 10);
        Assert.True(start.IsValid);
        ValidatedRunState run = start.Value;

        while (run.State.Status == RunStatus.Active)
        {
            string actionId = Rotation[(run.State.Week - 1) % Rotation.Length];
            WeekPlanResult result = WeekEngine.PlanWeek(run, catalog, actionId);
            Assert.True(result.IsSuccess, result.ErrorCode);
            RunStateValidation validation = RunStateValidator.Validate(result.Value.State, catalog.Parameters);
            Assert.True(validation.IsValid, string.Join(", ", validation.Errors));
            run = result.Value;
        }

        Assert.Equal(RunStatus.Completed, run.State.Status);
        Assert.Equal(52, run.State.History.Count);
        long expectedOpeningPln = catalog.Parameters.StartingMoneyPln;
        foreach (WeekRecord record in run.State.History)
        {
            WeekCashSummary summary = WeekCashSummary.For(run, record.Week);
            Assert.Equal(expectedOpeningPln, summary.OpeningMoneyPln);
            Assert.Equal(
                summary.OpeningMoneyPln + summary.SponsorsPln + summary.DonationsPln + summary.SubscriptionsPln - summary.ExpensesPln,
                summary.ClosingMoneyPln);
            expectedOpeningPln = summary.ClosingMoneyPln;
        }

        Assert.Equal(run.State.MoneyPln, expectedOpeningPln);
    }

    [Fact]
    public void BankruptRunStopsAdvancing()
    {
        GameParameters parameters = RunStateFixtures.InDebtParameters with { StartingViewers = 0 };
        ValidatedCatalog catalog = DraftCatalog.SingleAction(parameters, costPln: 60, viewersDelta: 0, dramaDelta: 0);
        RunStateValidation start = RunStarter.Start(catalog, RunStateFixtures.RunId, "NeonBorsuk", seed: 1, stream: 1);
        Assert.True(start.IsValid);

        WeekPlanResult first = WeekEngine.PlanWeek(start.Value, catalog, "test_action");
        Assert.True(first.IsSuccess);
        WeekPlanResult second = WeekEngine.PlanWeek(first.Value, catalog, "test_action");

        Assert.Equal(RunStatus.Bankrupt, first.Value.State.Status);
        Assert.Equal(WeekPlanErrorCodes.RunFinished, second.ErrorCode);
    }
}
