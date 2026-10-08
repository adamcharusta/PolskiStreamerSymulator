using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Randomness;
using PolskiStreamerSymulatorApp.Domain.Runs;

namespace PolskiStreamerSymulatorApp.Domain.Engine;

/// <summary>
/// Plays one week of a validated run against its pinned, validated catalogue. The same inputs always give the same result,
/// an error leaves no partial change, the input's lists are never mutated, and the new state gets new top-level lists;
/// finished week records are shared with the input because they never change.
/// Until S3 adds events, a week ends straight after its weekly action step.
/// </summary>
public static class WeekEngine
{
    private const int ActionStep = 0;

    public static WeekPlanResult PlanWeek(ValidatedRunState run, ValidatedCatalog catalog, string actionId)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(actionId);

        RunState state = run.State;
        if (state.CatalogVersion != catalog.Version || run.Parameters != catalog.Parameters)
        {
            return WeekPlanResult.Failure(WeekPlanErrorCodes.CatalogMismatch);
        }

        if (state.Status == RunStatus.PendingWeek)
        {
            return WeekPlanResult.Failure(WeekPlanErrorCodes.WeekPending);
        }

        if (state.Status != RunStatus.Active)
        {
            return WeekPlanResult.Failure(WeekPlanErrorCodes.RunFinished);
        }

        if (!catalog.TryGetAction(actionId, out WeeklyActionDefinition? action))
        {
            return WeekPlanResult.Failure(WeekPlanErrorCodes.UnknownAction);
        }

        GameParameters parameters = catalog.Parameters;
        int week = state.Week;
        List<CashFlowEntry> entries = [];

        // The guaranteed cost is paid before the roll, even with no cash on hand.
        if (action.GuaranteedCostPln > 0)
        {
            entries.Add(new CashFlowEntry(
                week, ActionStep, CashFlowSource.WeeklyActionCost, Ordinal: 0, CashFlowCategory.Expenses, action.GuaranteedCostPln));
        }

        Pcg32 generator = new(state.Rng);
        int roll = generator.NextRoll();
        WeeklyActionOutcome outcome = OutcomeTable.Select(action.Outcomes, roll);
        for (int ordinal = 0; ordinal < outcome.CashFlows.Count; ordinal++)
        {
            CashFlowAmount cashFlow = outcome.CashFlows[ordinal];
            entries.Add(new CashFlowEntry(
                week, ActionStep, CashFlowSource.WeeklyActionOutcome, ordinal, cashFlow.Category, cashFlow.AmountPln));
        }

        long viewers = Math.Max(0L, (long)state.Viewers + outcome.ViewersDelta);
        if (viewers > int.MaxValue)
        {
            return WeekPlanResult.Failure(WeekPlanErrorCodes.ValueOutOfRange);
        }

        int drama = Math.Clamp(state.Drama + outcome.DramaDelta, 0, GameParametersValidator.MaxDrama);
        long subscription = SubscriptionSettlement.WeeklyAmountPln((int)viewers, parameters.SubscriptionViewersPerPln);
        entries.Add(new CashFlowEntry(
            week, ActionStep, CashFlowSource.Subscription, Ordinal: 0, CashFlowCategory.Subscriptions, subscription));

        // The whole step reconciles before the bankruptcy check, so the outcome and the subscription can rescue the run.
        Int128 money = state.MoneyPln;
        foreach (CashFlowEntry entry in entries)
        {
            money += entry.SignedAmountPln;
        }

        if (money < long.MinValue || money > long.MaxValue)
        {
            return WeekPlanResult.Failure(WeekPlanErrorCodes.ValueOutOfRange);
        }

        ActionResolution resolution = new(
            action.ActionId, roll, outcome.OutcomeId, (int)viewers - state.Viewers, drama - state.Drama);
        WeekRecord record = new(week, resolution, EncounterRolls: [], Events: []);
        RunState next = CompleteWeek(state, parameters, record, entries, (long)money, (int)viewers, drama, generator.ToState());
        return WeekPlanResult.Success(new ValidatedRunState(next, parameters));
    }

    /// <summary>
    /// Records a finished week and decides what comes next. S3 moves the bankruptcy check to the end of the action step,
    /// then inserts the event phase between that check and this step.
    /// </summary>
    private static RunState CompleteWeek(
        RunState state,
        GameParameters parameters,
        WeekRecord record,
        IReadOnlyList<CashFlowEntry> newEntries,
        long money,
        int viewers,
        int drama,
        RngState rng)
    {
        RunStatus status;
        int nextWeek;
        RunEnding? ending;
        if (money <= parameters.BankruptcyThresholdPln)
        {
            // Bankruptcy takes priority over ordinary completion, also in the final week.
            status = RunStatus.Bankrupt;
            nextWeek = record.Week;
            ending = new RunEnding(RunEndingKind.Bankrupt, record.Week, ReasonCode: null);
        }
        else if (record.Week == parameters.RunLengthWeeks)
        {
            status = RunStatus.Completed;
            nextWeek = record.Week;
            ending = new RunEnding(RunEndingKind.Completed, record.Week, ReasonCode: null);
        }
        else
        {
            status = RunStatus.Active;
            nextWeek = record.Week + 1;
            ending = null;
        }

        return state with
        {
            Week = nextWeek,
            Status = status,
            MoneyPln = money,
            Viewers = viewers,
            Drama = drama,
            Rng = rng,
            Ledger = [.. state.Ledger, .. newEntries],
            History = [.. state.History, record],
            CurrentWeek = null,
            Flags = [.. state.Flags],
            Ending = ending,
        };
    }
}
