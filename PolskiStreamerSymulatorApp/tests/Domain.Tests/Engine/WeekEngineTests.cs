using System.Reflection;
using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Engine;
using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Randomness;
using PolskiStreamerSymulatorApp.Domain.Runs;
using PolskiStreamerSymulatorApp.Domain.Tests.Catalog;
using PolskiStreamerSymulatorApp.Domain.Tests.Runs;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Engine;

public sealed class WeekEngineTests
{
    private const string TestAction = "test_action";

    private static readonly Dictionary<string, Func<(ValidatedRunState Run, ValidatedCatalog Catalog, string ActionId, string Code)>> Guards = new()
    {
        ["catalogue version differs"] = static () =>
            (Start(DraftCatalog.Validated()), Revalidate(DraftCatalog.Catalog() with { Version = 2 }), "regular_stream",
                WeekPlanErrorCodes.CatalogMismatch),
        ["catalogue parameters differ"] = static () =>
            (Start(DraftCatalog.Validated()),
                DraftCatalog.Validated(RunStateFixtures.Parameters with { SubscriptionViewersPerPln = 5 }),
                "regular_stream",
                WeekPlanErrorCodes.CatalogMismatch),
        ["pending week"] = static () =>
            (Validate(RunStateFixtures.PendingWeek(), RunStateFixtures.Parameters), DraftCatalog.Validated(), "regular_stream",
                WeekPlanErrorCodes.WeekPending),
        ["completed run"] = static () =>
            (Validate(RunStateFixtures.Completed(), RunStateFixtures.TwoWeekParameters),
                DraftCatalog.Validated(RunStateFixtures.TwoWeekParameters),
                "regular_stream",
                WeekPlanErrorCodes.RunFinished),
        ["bankrupt run"] = static () =>
            (Validate(RunStateFixtures.Bankrupt(), RunStateFixtures.InDebtParameters),
                DraftCatalog.Validated(RunStateFixtures.InDebtParameters),
                "regular_stream",
                WeekPlanErrorCodes.RunFinished),
        ["special ending"] = static () =>
            (Validate(RunStateFixtures.SpecialEnding(), RunStateFixtures.Parameters), DraftCatalog.Validated(), "regular_stream",
                WeekPlanErrorCodes.RunFinished),
        ["unknown action"] = static () =>
            (Start(DraftCatalog.Validated()), DraftCatalog.Validated(), "streaming_marathon", WeekPlanErrorCodes.UnknownAction),
        ["money beyond 64 bits"] = static () => OutOfRange(
            RunStateFixtures.Parameters with { StartingMoneyPln = long.MaxValue }, viewersDelta: 0, DraftCatalog.Donations(80)),
        ["viewers beyond 32 bits"] = static () => OutOfRange(
            RunStateFixtures.Parameters with { StartingViewers = int.MaxValue }, viewersDelta: 45),
    };

    public static TheoryData<string> GuardNames => new(Guards.Keys);

    [Fact]
    public void FirstWeekMatchesTheKnownAnswer()
    {
        // Seed 42 on stream 54 rolls 1,783 first, which lands on the steady outcome of a regular stream.
        Pcg32 generator = new(Pcg32.Seed(42, 54));
        generator.NextRoll();
        RunState expected = RunStateFixtures.Active() with
        {
            Rng = generator.ToState(),
            History = [RunStateFixtures.QuietFirstWeek() with { EncounterRolls = [] }],
        };

        RunState actual = Plan(Start(DraftCatalog.Validated()), DraftCatalog.Validated(), "regular_stream");

        RunStateAssert.Equivalent(expected, actual);
    }

    [Fact]
    public void FreeActionRecordsOnlyTheSubscription()
    {
        RunState state = Plan(Start(DraftCatalog.Validated()), DraftCatalog.Validated(), "regular_stream");

        Assert.Equal([RunStateFixtures.Subscription(1, 2)], state.Ledger);
    }

    [Fact]
    public void PaidActionRecordsItsCostOnceBeforeTheOutcome()
    {
        // Roll 1,783 lands on the deal outcome of a sponsor pitch.
        RunState state = Plan(Start(DraftCatalog.Validated()), DraftCatalog.Validated(), "sponsor_pitch");

        Assert.Equal(
            [
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionCost, 0, CashFlowCategory.Expenses, 50),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 0, CashFlowCategory.Sponsors, 250),
                RunStateFixtures.Subscription(1, 2),
            ],
            state.Ledger);
        Assert.Equal(1_702, state.MoneyPln);
    }

    [Fact]
    public void OutcomeCashFlowsKeepTheirOrdinals()
    {
        ValidatedCatalog catalog = DraftCatalog.SingleAction(
            RunStateFixtures.Parameters, costPln: 0, viewersDelta: 0, dramaDelta: 0, DraftCatalog.Sponsors(100), DraftCatalog.Expense(30));

        RunState state = Plan(Start(catalog), catalog, TestAction);

        Assert.Equal(
            [
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 0, CashFlowCategory.Sponsors, 100),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 1, CashFlowCategory.Expenses, 30),
                RunStateFixtures.Subscription(1, 2),
            ],
            state.Ledger);
        Assert.Equal(1_572, state.MoneyPln);
    }

    [Fact]
    public void SubscriptionIsRecordedEvenAtZeroPln()
    {
        ValidatedCatalog catalog = DraftCatalog.SingleAction(
            RunStateFixtures.Parameters with { StartingViewers = 9 }, costPln: 0, viewersDelta: 0, dramaDelta: 0);

        RunState state = Plan(Start(catalog), catalog, TestAction);

        Assert.Equal([RunStateFixtures.Subscription(1, 0)], state.Ledger);
    }

    [Theory]
    [InlineData(3, -10, 0, -3)]
    [InlineData(20, 15, 35, 15)]
    public void ViewersStayAtOrAboveZero(int startingViewers, int outcomeDelta, int expectedViewers, int expectedAppliedDelta)
    {
        ValidatedCatalog catalog = DraftCatalog.SingleAction(
            RunStateFixtures.Parameters with { StartingViewers = startingViewers }, costPln: 0, viewersDelta: outcomeDelta, dramaDelta: 0);

        RunState state = Plan(Start(catalog), catalog, TestAction);

        Assert.Equal(expectedViewers, state.Viewers);
        Assert.Equal(expectedAppliedDelta, state.History[0].Action.ViewersDelta);
    }

    [Theory]
    [InlineData(95, 12, 100, 5)]
    [InlineData(2, -5, 0, -2)]
    [InlineData(50, -20, 30, -20)]
    public void DramaStaysWithinZeroToOneHundred(int startingDrama, int outcomeDelta, int expectedDrama, int expectedAppliedDelta)
    {
        ValidatedCatalog catalog = DraftCatalog.SingleAction(
            RunStateFixtures.Parameters with { StartingDrama = startingDrama }, costPln: 0, viewersDelta: 0, dramaDelta: outcomeDelta);

        RunState state = Plan(Start(catalog), catalog, TestAction);

        Assert.Equal(expectedDrama, state.Drama);
        Assert.Equal(expectedAppliedDelta, state.History[0].Action.DramaDelta);
    }

    [Fact]
    public void OutcomeAndSubscriptionRescueADebtFundedCost()
    {
        // -950 - 100 = -1,050 after the cost; +80 donations and 6 PLN of subscriptions close the step at -964.
        ValidatedCatalog catalog = DraftCatalog.SingleAction(
            RunStateFixtures.InDebtParameters, costPln: 100, viewersDelta: 45, dramaDelta: 0, DraftCatalog.Donations(80));

        RunState state = Plan(Start(catalog), catalog, TestAction);

        Assert.Equal(RunStatus.Active, state.Status);
        Assert.Equal(-964, state.MoneyPln);
        Assert.Equal(2, state.Week);
    }

    [Theory]
    [InlineData(50, RunStatus.Bankrupt, -1_000)]
    [InlineData(49, RunStatus.Active, -999)]
    public void StepClosingAtTheThresholdBankruptsTheRun(long costPln, RunStatus expectedStatus, long expectedMoneyPln)
    {
        ValidatedCatalog catalog = DraftCatalog.SingleAction(
            RunStateFixtures.InDebtParameters with { StartingViewers = 0 }, costPln, viewersDelta: 0, dramaDelta: 0);

        RunState state = Plan(Start(catalog), catalog, TestAction);

        Assert.Equal(expectedStatus, state.Status);
        Assert.Equal(expectedMoneyPln, state.MoneyPln);
        Assert.Equal(expectedStatus == RunStatus.Bankrupt ? new RunEnding(RunEndingKind.Bankrupt, 1, null) : null, state.Ending);
        Assert.Equal(expectedStatus == RunStatus.Bankrupt ? 1 : 2, state.Week);
    }

    [Fact]
    public void BankruptcyTakesPriorityInTheFinalWeek()
    {
        ValidatedCatalog catalog = DraftCatalog.SingleAction(
            RunStateFixtures.InDebtParameters with { RunLengthWeeks = 1, StartingViewers = 0 }, costPln: 50, viewersDelta: 0, dramaDelta: 0);

        RunState state = Plan(Start(catalog), catalog, TestAction);

        Assert.Equal(RunStatus.Bankrupt, state.Status);
        Assert.Equal(new RunEnding(RunEndingKind.Bankrupt, 1, null), state.Ending);
    }

    [Fact]
    public void FinalWeekCompletesTheRun()
    {
        GameParameters oneWeek = RunStateFixtures.Parameters with { RunLengthWeeks = 1 };

        RunState state = Plan(Start(DraftCatalog.Validated(oneWeek)), DraftCatalog.Validated(oneWeek), "regular_stream");

        Assert.Equal(RunStatus.Completed, state.Status);
        Assert.Equal(1, state.Week);
        Assert.Equal(new RunEnding(RunEndingKind.Completed, 1, null), state.Ending);
        Assert.Single(state.History);
    }

    [Theory]
    [MemberData(nameof(GuardNames))]
    public void GuardRejectsTheCommandWithoutChangingTheRun(string name)
    {
        (ValidatedRunState run, ValidatedCatalog catalog, string actionId, string code) = Guards[name]();
        int ledgerCount = run.State.Ledger.Count;
        int historyCount = run.State.History.Count;

        WeekPlanResult result = WeekEngine.PlanWeek(run, catalog, actionId);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal(code, result.ErrorCode);
        Assert.Equal((ledgerCount, historyCount), (run.State.Ledger.Count, run.State.History.Count));
    }

    [Fact]
    public void PlanningNeitherMutatesNorKeepsTheInputLists()
    {
        ValidatedRunState run = Validate(RunStateFixtures.Active(), RunStateFixtures.Parameters);
        CashFlowEntry[] ledgerBefore = [.. run.State.Ledger];
        WeekRecord[] historyBefore = [.. run.State.History];

        RunState next = Plan(run, DraftCatalog.Validated(), "quiet_week");

        Assert.Equal(ledgerBefore, run.State.Ledger);
        Assert.Equal(historyBefore, run.State.History);
        Assert.NotSame(run.State.Ledger, next.Ledger);
        Assert.NotSame(run.State.History, next.History);
    }

    [Fact]
    public void SameInputGivesTheSameResult()
    {
        ValidatedRunState run = Validate(RunStateFixtures.Active(), RunStateFixtures.Parameters);
        ValidatedCatalog catalog = DraftCatalog.Validated();

        RunState first = Plan(run, catalog, "provocative_stunt");
        RunState second = Plan(run, catalog, "provocative_stunt");

        RunStateAssert.Equivalent(first, second);
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(2_026UL)]
    [InlineData(ulong.MaxValue)]
    public void EngineConsumesExactlyOneRollFromTheRunGenerator(ulong seed)
    {
        ValidatedCatalog catalog = DraftCatalog.Validated();
        ValidatedRunState run = Start(catalog, seed);
        Pcg32 independent = new(run.State.Rng);
        int expectedRoll = independent.NextRoll();
        string expectedOutcome = OutcomeTable.Select(DraftCatalog.WeeklyActions[1].Outcomes, expectedRoll).OutcomeId;

        RunState state = Plan(run, catalog, "provocative_stunt");

        Assert.Equal(expectedRoll, state.History[0].Action.Roll);
        Assert.Equal(expectedOutcome, state.History[0].Action.OutcomeId);
        Assert.Equal(independent.ToState(), state.Rng);
    }

    [Fact]
    public void NullArgumentsAreRejected()
    {
        ValidatedCatalog catalog = DraftCatalog.Validated();
        ValidatedRunState run = Start(catalog);

        Assert.Throws<ArgumentNullException>(() => WeekEngine.PlanWeek(null!, catalog, "regular_stream"));
        Assert.Throws<ArgumentNullException>(() => WeekEngine.PlanWeek(run, null!, "regular_stream"));
        Assert.Throws<ArgumentNullException>(() => WeekEngine.PlanWeek(run, catalog, null!));
    }

    [Fact]
    public void AllListsEveryDeclaredCode()
    {
        string[] declared =
        [
            .. typeof(WeekPlanErrorCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(static field => field.IsLiteral)
                .Select(static field => (string)field.GetRawConstantValue()!),
        ];

        Assert.Equal(declared.Order(StringComparer.Ordinal), WeekPlanErrorCodes.All.Order(StringComparer.Ordinal));
    }

    /// <summary>Plans one week, requires success, and checks that the result passes the run-state validator.</summary>
    private static RunState Plan(ValidatedRunState run, ValidatedCatalog catalog, string actionId)
    {
        WeekPlanResult result = WeekEngine.PlanWeek(run, catalog, actionId);
        Assert.True(result.IsSuccess, result.ErrorCode);
        RunStateValidation validation = RunStateValidator.Validate(result.Value.State, catalog.Parameters);
        Assert.True(validation.IsValid, string.Join(", ", validation.Errors));
        return result.Value.State;
    }

    private static ValidatedRunState Start(ValidatedCatalog catalog, ulong seed = 42)
    {
        RunStateValidation start = RunStarter.Start(catalog, RunStateFixtures.RunId, "NeonBorsuk", seed, stream: 54);
        Assert.True(start.IsValid, string.Join(", ", start.Errors));
        return start.Value;
    }

    private static ValidatedRunState Validate(RunState state, GameParameters parameters)
    {
        RunStateValidation validation = RunStateValidator.Validate(state, parameters);
        Assert.True(validation.IsValid, string.Join(", ", validation.Errors));
        return validation.Value;
    }

    private static ValidatedCatalog Revalidate(GameCatalog catalog)
    {
        CatalogValidation validation = GameCatalogValidator.Validate(catalog);
        Assert.True(validation.IsValid, string.Join(", ", validation.Errors));
        return validation.Value;
    }

    private static (ValidatedRunState, ValidatedCatalog, string, string) OutOfRange(
        GameParameters parameters, int viewersDelta, params CashFlowAmount[] cashFlows)
    {
        ValidatedCatalog catalog = DraftCatalog.SingleAction(parameters, costPln: 0, viewersDelta, dramaDelta: 0, cashFlows);
        return (Start(catalog), catalog, TestAction, WeekPlanErrorCodes.ValueOutOfRange);
    }
}
