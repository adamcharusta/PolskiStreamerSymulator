using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Randomness;
using PolskiStreamerSymulatorApp.Domain.Runs;
using PolskiStreamerSymulatorApp.Domain.Versioning;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Runs;

public sealed class RunStateValidatorTests
{
    private static readonly Dictionary<string, Func<(RunState State, GameParameters Parameters)>> Mutations = new()
    {
        [RunStateErrorCodes.InvalidGameParameters] = static () =>
            (RunStateFixtures.Active(), RunStateFixtures.Parameters with { RunLengthWeeks = 0 }),
        [RunStateErrorCodes.UnsupportedRulesVersion] = static () =>
            (RunStateFixtures.Active() with { RulesVersion = RulesVersion.Current + 1 }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.InvalidCatalogVersion] = static () =>
            (RunStateFixtures.Active() with { CatalogVersion = 0 }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.InvalidRunId] = static () =>
            (RunStateFixtures.Active() with { RunId = Guid.Empty }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.InvalidStreamerName] = static () =>
            (RunStateFixtures.Active() with { StreamerName = " NeonBorsuk" }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.UnsupportedRngAlgorithm] = static () =>
            (RunStateFixtures.Active() with { Rng = Pcg32.Seed(42, 54) with { Algorithm = "xorshift" } }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.WeekOutOfRange] = static () =>
            (RunStateFixtures.Active(), RunStateFixtures.Parameters with { RunLengthWeeks = 1 }),
        [RunStateErrorCodes.ViewersNegative] = static () =>
            (RunStateFixtures.Active() with { Viewers = -1 }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.DramaOutOfRange] = static () =>
            (RunStateFixtures.Active() with { Drama = 101 }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.InvalidStableId] = static () =>
            WithFirstWeek(static week => week with { Action = week.Action with { ActionId = "Regular-Stream" } }),
        [RunStateErrorCodes.StatusInconsistent] = static () =>
            (RunStateFixtures.Active() with { Status = RunStatus.PendingWeek }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.EndingInconsistent] = static () =>
            (RunStateFixtures.Completed() with { Ending = new RunEnding(RunEndingKind.Completed, Week: 2, ReasonCode: "retired") },
                RunStateFixtures.TwoWeekParameters),
        [RunStateErrorCodes.MoneyAtOrBelowBankruptcyThreshold] = static () =>
            (RunStateFixtures.Active() with { MoneyPln = -1_000 }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.BankruptMoneyAboveThreshold] = static () =>
            (RunStateFixtures.Bankrupt() with { MoneyPln = -999 }, RunStateFixtures.InDebtParameters),
        [RunStateErrorCodes.HistoryInconsistent] = static () =>
            (RunStateFixtures.Active() with { History = [] }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.EventCapExceeded] = static () =>
            (TwoEventPendingWeek(), RunStateFixtures.Parameters with { MaxEventsPerWeek = 1 }),
        [RunStateErrorCodes.EventIndexInconsistent] = static () =>
            WithCurrentWeek(static week => week with { Pending = week.Pending with { Index = 2 } }),
        [RunStateErrorCodes.EventSelectedTwice] = static () =>
            WithCurrentWeek(static week => week with
            {
                ResolvedEvents = [AnsweredEvent("internet_outage", 1)],
                Pending = week.Pending with { Index = 2 },
            }),
        [RunStateErrorCodes.EncounterRolledTwice] = static () =>
            WithCurrentWeek(static week => week with
            {
                EncounterRolls = [.. week.EncounterRolls, new EncounterRoll("internet_outage", 7_000, false)],
            }),
        [RunStateErrorCodes.SelectedEventWithoutPassingRoll] = static () =>
            WithCurrentWeek(static week => week with { EncounterRolls = [new EncounterRoll("internet_outage", 120, false)] }),
        [RunStateErrorCodes.UnansweredEvent] = static () =>
            WithFirstWeek(static week => week with
            {
                EncounterRolls = [new EncounterRoll("meme_misread", 300, true)],
                Events = [new EventResolution("meme_misread", 1, Response: null)],
            }),
        [RunStateErrorCodes.TerminalReasonNotApplied] = static () =>
            WithFirstWeek(static week => week with
            {
                EncounterRolls = [new EncounterRoll("meme_misread", 300, true)],
                Events = [new EventResolution("meme_misread", 1, Answer() with { TerminalReasonCode = "retired" })],
            }),
        [RunStateErrorCodes.RollOutOfRange] = static () =>
            WithFirstWeek(static week => week with { Action = week.Action with { Roll = 10_000 } }),
        [RunStateErrorCodes.LedgerWeekOutOfRange] = static () =>
            (RunStateFixtures.Active() with
            {
                Ledger =
                [
                    RunStateFixtures.Subscription(1, 2),
                    new CashFlowEntry(5, 0, CashFlowSource.WeeklyActionOutcome, 0, CashFlowCategory.Donations, 10),
                ],
            }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.LedgerStepInvalid] = static () =>
            (RunStateFixtures.Active() with
            {
                Ledger = [new CashFlowEntry(1, 1, CashFlowSource.Subscription, 0, CashFlowCategory.Subscriptions, 2)],
            }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.LedgerCategoryMismatch] = static () =>
            (RunStateFixtures.Active() with
            {
                Ledger = [new CashFlowEntry(1, 0, CashFlowSource.Subscription, 0, CashFlowCategory.Sponsors, 2)],
            }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.LedgerAmountInvalid] = static () =>
            WithExtraPendingWeekEntry(new CashFlowEntry(2, 0, CashFlowSource.WeeklyActionOutcome, 1, CashFlowCategory.Donations, 0)),
        [RunStateErrorCodes.LedgerDuplicateEntry] = static () =>
            WithExtraPendingWeekEntry(new CashFlowEntry(2, 0, CashFlowSource.WeeklyActionOutcome, 0, CashFlowCategory.Donations, 20)),
        [RunStateErrorCodes.SubscriptionEntryCount] = static () =>
            (RunStateFixtures.Active() with { Ledger = [] }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.MoneyNotReconciled] = static () =>
            (RunStateFixtures.Active() with { MoneyPln = 1_503 }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.ViewersNotReconciled] = static () =>
            (RunStateFixtures.Active() with { Viewers = 29 }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.DramaNotReconciled] = static () =>
            (RunStateFixtures.Active() with { Drama = 49 }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.FlagInvalid] = static () =>
            (RunStateFixtures.Active() with { Flags = [new NarrativeFlag("clip_backlash", SetWeek: 3)] }, RunStateFixtures.Parameters),
    };

    public static TheoryData<string> ValidFixtureNames => ["active", "pendingWeek", "completed", "bankrupt", "specialEnding"];

    public static TheoryData<string> ErrorCodes => new(Mutations.Keys);

    [Theory]
    [MemberData(nameof(ValidFixtureNames))]
    public void ValidFixturePassesAndYieldsAValidatedState(string name)
    {
        (RunState state, GameParameters parameters) = ValidFixture(name);

        RunStateValidation result = RunStateValidator.Validate(state, parameters);

        Assert.Empty(result.Errors);
        Assert.True(result.IsValid);
        Assert.NotNull(result.Value);
        Assert.Same(state, result.Value.State);
        Assert.Same(parameters, result.Value.Parameters);
    }

    [Theory]
    [MemberData(nameof(ErrorCodes))]
    public void SmallestMutationProducesItsErrorCode(string code)
    {
        (RunState state, GameParameters parameters) = Mutations[code]();

        RunStateValidation result = RunStateValidator.Validate(state, parameters);

        Assert.False(result.IsValid);
        Assert.Null(result.Value);
        Assert.Contains(code, result.Errors.Select(error => error.Code));
    }

    [Fact]
    public void EveryErrorCodeHasAMutation()
    {
        Assert.Equal(RunStateErrorCodes.All.Order(StringComparer.Ordinal), Mutations.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ReportsEveryErrorItFinds()
    {
        RunState state = RunStateFixtures.Active() with { Viewers = -1, Drama = 101 };

        RunStateValidation result = RunStateValidator.Validate(state, RunStateFixtures.Parameters);

        string[] codes = [.. result.Errors.Select(error => error.Code)];
        Assert.Contains(RunStateErrorCodes.ViewersNegative, codes);
        Assert.Contains(RunStateErrorCodes.DramaOutOfRange, codes);
    }

    [Fact]
    public void InvalidParametersStopValidationWithOneError()
    {
        RunState state = RunStateFixtures.Active() with { Viewers = -1 };

        RunStateValidation result = RunStateValidator.Validate(state, RunStateFixtures.Parameters with { RunLengthWeeks = 0 });

        RunStateError error = Assert.Single(result.Errors);
        Assert.Equal(new RunStateError(RunStateErrorCodes.InvalidGameParameters, "parameters"), error);
    }

    [Fact]
    public void ErrorsCarryThePathOfTheOffendingValue()
    {
        (RunState state, GameParameters parameters) = WithFirstWeek(static week => week with { Action = week.Action with { Roll = 10_000 } });

        RunStateValidation result = RunStateValidator.Validate(state, parameters);

        RunStateError error = Assert.Single(result.Errors);
        Assert.Equal(new RunStateError(RunStateErrorCodes.RollOutOfRange, "history[0].action.roll"), error);
    }

    [Fact]
    public void LedgerAmountsThatWrapAroundSixtyFourBitsDoNotReconcile()
    {
        // In 64-bit arithmetic these three amounts sum to exactly zero, so a wrapping sum would accept the money total.
        RunState state = RunStateFixtures.Active() with
        {
            Ledger =
            [
                RunStateFixtures.Subscription(1, 2),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 0, CashFlowCategory.Sponsors, long.MaxValue),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 1, CashFlowCategory.Donations, long.MaxValue),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 2, CashFlowCategory.Donations, 2),
            ],
        };

        RunStateValidation result = RunStateValidator.Validate(state, RunStateFixtures.Parameters);

        Assert.Contains(RunStateErrorCodes.MoneyNotReconciled, result.Errors.Select(error => error.Code));
    }

    [Fact]
    public void BankruptRunWhoseUnansweredEventHasNoEncounterCostIsUnanswered()
    {
        // The 60 PLN moves from an encounter cost to a weekly action cost, so money still reconciles.
        RunState state = RunStateFixtures.Bankrupt() with
        {
            Ledger =
            [
                RunStateFixtures.Subscription(1, 2),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionCost, 0, CashFlowCategory.Expenses, 60),
            ],
        };
        GameParameters parameters = RunStateFixtures.InDebtParameters;

        RunStateValidation result = RunStateValidator.Validate(state, parameters);

        RunStateError error = Assert.Single(result.Errors);
        Assert.Equal(new RunStateError(RunStateErrorCodes.UnansweredEvent, "history[0].events[0]"), error);
    }

    [Fact]
    public void FreshRunBeforeWeekOneIsValid()
    {
        GameParameters parameters = RunStateFixtures.Parameters;
        RunState state = new(
            RulesVersion: RulesVersion.Current,
            CatalogVersion: 1,
            RunId: RunStateFixtures.RunId,
            StreamerName: "NeonBorsuk",
            Week: 1,
            Status: RunStatus.Active,
            MoneyPln: parameters.StartingMoneyPln,
            Viewers: parameters.StartingViewers,
            Drama: parameters.StartingDrama,
            Rng: Pcg32.Seed(42, 54),
            Ledger: [],
            History: [],
            CurrentWeek: null,
            Flags: [],
            Ending: null);

        RunStateValidation result = RunStateValidator.Validate(state, parameters);

        Assert.Empty(result.Errors);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ZeroSubscriptionEntryIsValid()
    {
        RunState state = RunStateFixtures.Active() with
        {
            MoneyPln = 1_500,
            Ledger = [RunStateFixtures.Subscription(1, 0)],
        };

        RunStateValidation result = RunStateValidator.Validate(state, RunStateFixtures.Parameters);

        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void ZeroAmountOnAnyOtherPendingWeekEntryIsInvalid(int ledgerIndex)
    {
        RunState state = RunStateFixtures.PendingWeek();
        CashFlowEntry[] ledger = [.. state.Ledger];
        ledger[ledgerIndex] = ledger[ledgerIndex] with { AmountPln = 0 };

        RunStateValidation result = RunStateValidator.Validate(state with { Ledger = ledger }, RunStateFixtures.Parameters);

        Assert.Contains(new RunStateError(RunStateErrorCodes.LedgerAmountInvalid, $"ledger[{ledgerIndex}]"), result.Errors);
    }

    [Theory]
    [InlineData(CashFlowSource.ResponseCost, CashFlowCategory.Expenses)]
    [InlineData(CashFlowSource.ResponseOutcome, CashFlowCategory.Donations)]
    public void ZeroAmountOnAResponseEntryIsInvalid(CashFlowSource source, CashFlowCategory category)
    {
        RunState state = RunStateFixtures.SpecialEnding();
        CashFlowEntry entry = new(1, 1, source, 0, category, 0);

        RunStateValidation result = RunStateValidator.Validate(
            state with { Ledger = [.. state.Ledger, entry] },
            RunStateFixtures.Parameters);

        Assert.Contains(new RunStateError(RunStateErrorCodes.LedgerAmountInvalid, "ledger[1]"), result.Errors);
    }

    [Fact]
    public void BankruptRunWhoseFinalResponseCarriesATerminalReasonIsValid()
    {
        RunState state = RunStateFixtures.Bankrupt();
        WeekRecord week = state.History[0];
        EventResolution answered = week.Events[0] with
        {
            Response = Answer() with { ViewersDelta = 0, DramaDelta = 0, TerminalReasonCode = "debt_collapse" },
        };
        state = state with { History = [week with { Events = [answered] }] };

        RunStateValidation result = RunStateValidator.Validate(state, RunStateFixtures.InDebtParameters);

        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(CashFlowSource.ResponseCost, CashFlowCategory.Expenses)]
    [InlineData(CashFlowSource.ResponseOutcome, CashFlowCategory.Donations)]
    public void ResponseEntryOnAnUnansweredEventIsInvalid(CashFlowSource source, CashFlowCategory category)
    {
        RunState state = RunStateFixtures.Bankrupt();
        CashFlowEntry entry = new(1, 1, source, 0, category, 10);

        RunStateValidation result = RunStateValidator.Validate(
            state with { Ledger = [.. state.Ledger, entry] },
            RunStateFixtures.InDebtParameters);

        Assert.Contains(new RunStateError(RunStateErrorCodes.LedgerStepInvalid, "ledger[2].step"), result.Errors);
    }

    [Theory]
    [InlineData(CashFlowSource.ResponseCost, CashFlowCategory.Expenses)]
    [InlineData(CashFlowSource.ResponseOutcome, CashFlowCategory.Donations)]
    public void ResponseEntryOnAStepWithoutAnEventIsInvalid(CashFlowSource source, CashFlowCategory category)
    {
        RunState state = RunStateFixtures.Active();
        CashFlowEntry entry = new(1, 1, source, 0, category, 10);

        RunStateValidation result = RunStateValidator.Validate(
            state with { Ledger = [.. state.Ledger, entry] },
            RunStateFixtures.Parameters);

        Assert.Contains(new RunStateError(RunStateErrorCodes.LedgerStepInvalid, "ledger[1].step"), result.Errors);
    }

    [Fact]
    public void SpecialEndingWhoseReasonDiffersFromTheFinalResponseIsInconsistent()
    {
        RunState state = RunStateFixtures.SpecialEnding() with
        {
            Ending = new RunEnding(RunEndingKind.Special, Week: 1, ReasonCode: "other_reason"),
        };

        RunStateValidation result = RunStateValidator.Validate(state, RunStateFixtures.Parameters);

        RunStateError error = Assert.Single(result.Errors);
        Assert.Equal(new RunStateError(RunStateErrorCodes.EndingInconsistent, "ending.reasonCode"), error);
    }

    private static (RunState State, GameParameters Parameters) ValidFixture(string name)
    {
        return name switch
        {
            "active" => (RunStateFixtures.Active(), RunStateFixtures.Parameters),
            "pendingWeek" => (RunStateFixtures.PendingWeek(), RunStateFixtures.Parameters),
            "completed" => (RunStateFixtures.Completed(), RunStateFixtures.TwoWeekParameters),
            "bankrupt" => (RunStateFixtures.Bankrupt(), RunStateFixtures.InDebtParameters),
            "specialEnding" => (RunStateFixtures.SpecialEnding(), RunStateFixtures.Parameters),
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown fixture."),
        };
    }

    private static (RunState State, GameParameters Parameters) WithFirstWeek(Func<WeekRecord, WeekRecord> change)
    {
        RunState state = RunStateFixtures.Active();
        return (state with { History = [change(state.History[0])] }, RunStateFixtures.Parameters);
    }

    private static (RunState State, GameParameters Parameters) WithCurrentWeek(Func<WeekInProgress, WeekInProgress> change)
    {
        RunState state = RunStateFixtures.PendingWeek();
        return (state with { CurrentWeek = change(state.CurrentWeek!) }, RunStateFixtures.Parameters);
    }

    private static (RunState State, GameParameters Parameters) WithExtraPendingWeekEntry(CashFlowEntry entry)
    {
        RunState state = RunStateFixtures.PendingWeek();
        return (state with { Ledger = [.. state.Ledger, entry] }, RunStateFixtures.Parameters);
    }

    private static RunState TwoEventPendingWeek()
    {
        RunState state = RunStateFixtures.PendingWeek();
        WeekInProgress week = state.CurrentWeek!;
        return state with
        {
            CurrentWeek = week with
            {
                EncounterRolls = [.. week.EncounterRolls, new EncounterRoll("mysterious_donation", 200, true)],
                ResolvedEvents = [AnsweredEvent("mysterious_donation", 1)],
                Pending = week.Pending with { Index = 2 },
            },
        };
    }

    private static ResponseResolution Answer()
    {
        return new ResponseResolution(
            OptionId: "polite_refusal",
            Roll: 100,
            OutcomeId: "accepted",
            ViewersDelta: 2,
            DramaDelta: -1,
            FlagChanges: [],
            TerminalReasonCode: null);
    }

    private static EventResolution AnsweredEvent(string eventId, int index)
    {
        return new EventResolution(eventId, index, Answer());
    }
}
