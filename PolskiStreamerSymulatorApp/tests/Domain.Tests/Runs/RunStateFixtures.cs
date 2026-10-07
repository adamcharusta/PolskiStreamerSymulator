using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Randomness;
using PolskiStreamerSymulatorApp.Domain.Runs;
using PolskiStreamerSymulatorApp.Domain.Versioning;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Runs;

/// <summary>
/// Internally consistent run states, one per status, built from sample catalogue IDs.
/// </summary>
internal static class RunStateFixtures
{
    public static readonly Guid RunId = Guid.Parse("3f2b8c4e-6a1d-4c2e-9b7a-5d0e8f1a2b3c");

    public static GameParameters Parameters { get; } = new(
        RunLengthWeeks: 52,
        MaxEventsPerWeek: 3,
        StartingMoneyPln: 1_500,
        StartingViewers: 20,
        StartingDrama: 50,
        BankruptcyThresholdPln: -1_000,
        SubscriptionViewersPerPln: 10,
        ScoreAudienceReferenceViewers: 1_000,
        ScoreProfitReferencePln: 5_000,
        ScorePointsPerComponent: 500,
        CalmDramaMax: 33,
        MiddleDramaMax: 66);

    /// <summary>A two-week configuration, so a completed run stays short.</summary>
    public static GameParameters TwoWeekParameters { get; } = Parameters with { RunLengthWeeks = 2 };

    /// <summary>A configuration that starts in debt, so one encounter cost can bankrupt the run.</summary>
    public static GameParameters InDebtParameters { get; } = Parameters with { StartingMoneyPln = -950 };

    public static CashFlowEntry Subscription(int week, long amountPln)
    {
        return new CashFlowEntry(week, Step: 0, CashFlowSource.Subscription, Ordinal: 0, CashFlowCategory.Subscriptions, amountPln);
    }

    /// <summary>A regular stream with the steady outcome, one failed encounter roll, and no event.</summary>
    public static WeekRecord QuietFirstWeek()
    {
        return new WeekRecord(
            Week: 1,
            Action: new ActionResolution("regular_stream", Roll: 1_783, OutcomeId: "steady", ViewersDelta: 8, DramaDelta: 0),
            EncounterRolls: [new EncounterRoll("meme_misread", Roll: 9_001, Passed: false)],
            Events: []);
    }

    /// <summary>Week 2 is next: 1,502 PLN, 28 viewers, drama 50.</summary>
    public static RunState Active()
    {
        return new RunState(
            RulesVersion: RulesVersion.Current,
            CatalogVersion: 1,
            RunId: RunId,
            StreamerName: "NeonBorsuk",
            Week: 2,
            Status: RunStatus.Active,
            MoneyPln: 1_502,
            Viewers: 28,
            Drama: 50,
            Rng: Pcg32.Seed(42, 54),
            Ledger: [Subscription(1, 2)],
            History: [QuietFirstWeek()],
            CurrentWeek: null,
            Flags: [],
            Ending: null);
    }

    /// <summary>Week 2 waits for a response to an internet outage whose 30 PLN encounter cost is already paid.</summary>
    public static RunState PendingWeek()
    {
        return Active() with
        {
            Status = RunStatus.PendingWeek,
            MoneyPln = 1_675,
            Viewers = 33,
            Ledger =
            [
                Subscription(1, 2),
                new CashFlowEntry(2, 0, CashFlowSource.WeeklyActionCost, 0, CashFlowCategory.Expenses, 50),
                new CashFlowEntry(2, 0, CashFlowSource.WeeklyActionOutcome, 0, CashFlowCategory.Sponsors, 250),
                Subscription(2, 3),
                new CashFlowEntry(2, 1, CashFlowSource.EncounterCost, 0, CashFlowCategory.Expenses, 30),
            ],
            CurrentWeek = new WeekInProgress(
                Week: 2,
                Action: new ActionResolution("sponsor_pitch", Roll: 3_097, OutcomeId: "deal", ViewersDelta: 5, DramaDelta: 0),
                EncounterRolls: [new EncounterRoll("internet_outage", Roll: 120, Passed: true)],
                ResolvedEvents: [],
                Pending: new PendingEvent("internet_outage", Index: 1)),
        };
    }

    /// <summary>A two-week run that finished after two quiet regular streams. Validate with TwoWeekParameters.</summary>
    public static RunState Completed()
    {
        WeekRecord secondWeek = new(
            Week: 2,
            Action: new ActionResolution("regular_stream", Roll: 5_824, OutcomeId: "steady", ViewersDelta: 8, DramaDelta: 0),
            EncounterRolls: [],
            Events: []);
        return Active() with
        {
            Status = RunStatus.Completed,
            MoneyPln = 1_505,
            Viewers = 36,
            Ledger = [Subscription(1, 2), Subscription(2, 3)],
            History = [QuietFirstWeek(), secondWeek],
            Ending = new RunEnding(RunEndingKind.Completed, Week: 2, ReasonCode: null),
        };
    }

    /// <summary>Week 1 ended at a 60 PLN encounter cost that took money from -948 to -1,008. Validate with InDebtParameters.</summary>
    public static RunState Bankrupt()
    {
        WeekRecord week = QuietFirstWeek() with
        {
            EncounterRolls = [new EncounterRoll("rented_studio", Roll: 55, Passed: true)],
            Events = [new EventResolution("rented_studio", Index: 1, Response: null)],
        };
        return Active() with
        {
            Week = 1,
            Status = RunStatus.Bankrupt,
            MoneyPln = -1_008,
            Ledger = [Subscription(1, 2), new CashFlowEntry(1, 1, CashFlowSource.EncounterCost, 0, CashFlowCategory.Expenses, 60)],
            History = [week],
            Ending = new RunEnding(RunEndingKind.Bankrupt, Week: 1, ReasonCode: null),
        };
    }

    /// <summary>Week 1 ended in retirement after the streamer accepted a farewell offer.</summary>
    public static RunState SpecialEnding()
    {
        WeekRecord week = QuietFirstWeek() with
        {
            EncounterRolls = [new EncounterRoll("farewell_offer", Roll: 40, Passed: true)],
            Events =
            [
                new EventResolution(
                    "farewell_offer",
                    Index: 1,
                    Response: new ResponseResolution(
                        OptionId: "accept_offer",
                        Roll: 9_500,
                        OutcomeId: "retire_now",
                        ViewersDelta: -5,
                        DramaDelta: -10,
                        FlagChanges: [],
                        TerminalReasonCode: "retired")),
            ],
        };
        return Active() with
        {
            Week = 1,
            Status = RunStatus.SpecialEnding,
            Viewers = 23,
            Drama = 40,
            History = [week],
            Ending = new RunEnding(RunEndingKind.Special, Week: 1, ReasonCode: "retired"),
        };
    }
}
