using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Identity;
using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Randomness;
using PolskiStreamerSymulatorApp.Domain.Versioning;

namespace PolskiStreamerSymulatorApp.Domain.Runs;

/// <summary>
/// Checks an untrusted run state against the parameters of its pinned catalogue version before any game rule may use it.
/// It checks totals and structure, not a full replay of every historical step.
/// Every list and every non-nullable member must be non-null; the strict JSON contract guarantees that before mapping.
/// </summary>
public static class RunStateValidator
{
    public const int MaxRoll = 9_999;
    public const int MaxDrama = 100;

    public static RunStateValidation Validate(RunState state, GameParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(parameters);

        if (GameParametersValidator.Validate(parameters).Count > 0)
        {
            return RunStateValidation.Failure([new RunStateError(RunStateErrorCodes.InvalidGameParameters, "parameters")]);
        }

        List<RunStateError> errors = [];
        ValidateIdentity(state, errors);
        ValidateStatistics(state, parameters, errors);
        ValidateStatusAndEnding(state, parameters, errors);
        List<WeekView> weeks = CollectWeeks(state, errors);
        foreach (WeekView week in weeks)
        {
            ValidateWeek(state, parameters, week, errors);
        }

        ValidateLedger(state, weeks, errors);
        ValidateReconciliation(state, parameters, weeks, errors);
        ValidateFlags(state, errors);

        return errors.Count == 0
            ? RunStateValidation.Success(new ValidatedRunState(state, parameters))
            : RunStateValidation.Failure(errors);
    }

    private static void ValidateIdentity(RunState state, List<RunStateError> errors)
    {
        if (state.RulesVersion != RulesVersion.Current)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.UnsupportedRulesVersion, "rulesVersion"));
        }

        if (state.CatalogVersion < 1)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.InvalidCatalogVersion, "catalogVersion"));
        }

        if (state.RunId == Guid.Empty)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.InvalidRunId, "runId"));
        }

        if (!StreamerName.IsNormalizedValid(state.StreamerName))
        {
            errors.Add(new RunStateError(RunStateErrorCodes.InvalidStreamerName, "streamerName"));
        }

        if (!string.Equals(state.Rng.Algorithm, Pcg32.AlgorithmId, StringComparison.Ordinal))
        {
            errors.Add(new RunStateError(RunStateErrorCodes.UnsupportedRngAlgorithm, "rng.algorithm"));
        }
    }

    private static void ValidateStatistics(RunState state, GameParameters parameters, List<RunStateError> errors)
    {
        if (state.Week < 1 || state.Week > parameters.RunLengthWeeks)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.WeekOutOfRange, "week"));
        }

        if (state.Viewers < 0)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.ViewersNegative, "viewers"));
        }

        if (state.Drama is < 0 or > MaxDrama)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.DramaOutOfRange, "drama"));
        }
    }

    private static void ValidateStatusAndEnding(RunState state, GameParameters parameters, List<RunStateError> errors)
    {
        RunEndingKind? expectedEnding = state.Status switch
        {
            RunStatus.Completed => RunEndingKind.Completed,
            RunStatus.Bankrupt => RunEndingKind.Bankrupt,
            RunStatus.SpecialEnding => RunEndingKind.Special,
            _ => null,
        };
        bool consistent = state.Status switch
        {
            RunStatus.Active => state.CurrentWeek is null && state.Ending is null,
            RunStatus.PendingWeek => state.CurrentWeek is not null && state.Ending is null,
            RunStatus.Completed or RunStatus.Bankrupt or RunStatus.SpecialEnding =>
                state.CurrentWeek is null && state.Ending is not null && state.Ending.Kind == expectedEnding,
            _ => false,
        };
        if (!consistent)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.StatusInconsistent, "status"));
        }

        if (state.Ending is { } ending)
        {
            if (ending.Week != state.Week || (ending.Kind == RunEndingKind.Completed && ending.Week != parameters.RunLengthWeeks))
            {
                errors.Add(new RunStateError(RunStateErrorCodes.EndingInconsistent, "ending.week"));
            }

            if (ending.Kind != RunEndingKind.Special)
            {
                if (ending.ReasonCode is not null)
                {
                    errors.Add(new RunStateError(RunStateErrorCodes.EndingInconsistent, "ending.reasonCode"));
                }
            }
            else if (ending.ReasonCode is null)
            {
                errors.Add(new RunStateError(RunStateErrorCodes.EndingInconsistent, "ending.reasonCode"));
            }
            else
            {
                ValidateId(ending.ReasonCode, "ending.reasonCode", errors);
            }
        }

        bool atOrBelowThreshold = state.MoneyPln <= parameters.BankruptcyThresholdPln;
        if (state.Status == RunStatus.Bankrupt && !atOrBelowThreshold)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.BankruptMoneyAboveThreshold, "moneyPln"));
        }
        else if (state.Status != RunStatus.Bankrupt && atOrBelowThreshold)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.MoneyAtOrBelowBankruptcyThreshold, "moneyPln"));
        }
    }

    private static List<WeekView> CollectWeeks(RunState state, List<RunStateError> errors)
    {
        bool terminal = state.Status is RunStatus.Completed or RunStatus.Bankrupt or RunStatus.SpecialEnding;
        int expectedHistoryCount = terminal ? state.Week : state.Week - 1;
        if (state.History.Count != expectedHistoryCount)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.HistoryInconsistent, "history"));
        }

        List<WeekView> weeks = [];
        for (int i = 0; i < state.History.Count; i++)
        {
            WeekRecord record = state.History[i];
            string path = $"history[{i}]";
            if (record.Week != i + 1)
            {
                errors.Add(new RunStateError(RunStateErrorCodes.HistoryInconsistent, $"{path}.week"));
            }

            bool isFinal = terminal && i == state.History.Count - 1;
            weeks.Add(new WeekView(record.Week, path, record.Action, record.EncounterRolls, record.Events, $"{path}.events", null, isFinal));
        }

        if (state.CurrentWeek is { } current)
        {
            if (current.Week != state.Week)
            {
                errors.Add(new RunStateError(RunStateErrorCodes.HistoryInconsistent, "currentWeek.week"));
            }

            weeks.Add(new WeekView(
                current.Week,
                "currentWeek",
                current.Action,
                current.EncounterRolls,
                current.ResolvedEvents,
                "currentWeek.resolvedEvents",
                current.Pending,
                IsFinal: false));
        }

        return weeks;
    }

    private static void ValidateWeek(RunState state, GameParameters parameters, WeekView week, List<RunStateError> errors)
    {
        string actionPath = $"{week.Path}.action";
        ValidateId(week.Action.ActionId, $"{actionPath}.actionId", errors);
        ValidateId(week.Action.OutcomeId, $"{actionPath}.outcomeId", errors);
        ValidateRoll(week.Action.Roll, $"{actionPath}.roll", errors);

        HashSet<string> passing = ValidateEncounterRolls(week, errors);
        HashSet<string> selected = new(StringComparer.Ordinal);
        for (int i = 0; i < week.Events.Count; i++)
        {
            EventResolution resolution = week.Events[i];
            string path = $"{week.EventsPath}[{i}]";
            bool isLastEvent = i == week.Events.Count - 1;
            ValidateSelection(resolution.EventId, resolution.Index, i + 1, path, passing, selected, errors);
            if (resolution.Response is null)
            {
                bool bankruptAtEncounter = week.IsFinal && isLastEvent && state.Status == RunStatus.Bankrupt;
                if (!bankruptAtEncounter)
                {
                    errors.Add(new RunStateError(RunStateErrorCodes.UnansweredEvent, path));
                }
            }
            else
            {
                ValidateResponse(state, week, resolution.Response, isLastEvent, $"{path}.response", errors);
            }
        }

        if (week.Pending is { } pending)
        {
            ValidateSelection(pending.EventId, pending.Index, week.Events.Count + 1, $"{week.Path}.pending", passing, selected, errors);
        }

        int eventCount = week.Events.Count + (week.Pending is null ? 0 : 1);
        if (eventCount > parameters.MaxEventsPerWeek)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.EventCapExceeded, week.Path));
        }

        if (week.IsFinal && state.Status == RunStatus.SpecialEnding)
        {
            string? finalReason = week.Events.Count > 0 ? week.Events[^1].Response?.TerminalReasonCode : null;
            if (finalReason is null || !string.Equals(finalReason, state.Ending?.ReasonCode, StringComparison.Ordinal))
            {
                errors.Add(new RunStateError(RunStateErrorCodes.EndingInconsistent, "ending.reasonCode"));
            }
        }
    }

    private static HashSet<string> ValidateEncounterRolls(WeekView week, List<RunStateError> errors)
    {
        HashSet<string> rolled = new(StringComparer.Ordinal);
        HashSet<string> passing = new(StringComparer.Ordinal);
        for (int i = 0; i < week.EncounterRolls.Count; i++)
        {
            EncounterRoll roll = week.EncounterRolls[i];
            string path = $"{week.Path}.encounterRolls[{i}]";
            ValidateId(roll.EventId, $"{path}.eventId", errors);
            ValidateRoll(roll.Roll, $"{path}.roll", errors);
            if (!rolled.Add(roll.EventId))
            {
                errors.Add(new RunStateError(RunStateErrorCodes.EncounterRolledTwice, path));
            }
            else if (roll.Passed)
            {
                passing.Add(roll.EventId);
            }
        }

        return passing;
    }

    private static void ValidateSelection(
        string eventId,
        int index,
        int expectedIndex,
        string path,
        HashSet<string> passing,
        HashSet<string> selected,
        List<RunStateError> errors)
    {
        ValidateId(eventId, $"{path}.eventId", errors);
        if (index != expectedIndex)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.EventIndexInconsistent, $"{path}.index"));
        }

        if (!selected.Add(eventId))
        {
            errors.Add(new RunStateError(RunStateErrorCodes.EventSelectedTwice, $"{path}.eventId"));
        }

        if (!passing.Contains(eventId))
        {
            errors.Add(new RunStateError(RunStateErrorCodes.SelectedEventWithoutPassingRoll, $"{path}.eventId"));
        }
    }

    private static void ValidateResponse(
        RunState state,
        WeekView week,
        ResponseResolution response,
        bool isLastEvent,
        string path,
        List<RunStateError> errors)
    {
        ValidateId(response.OptionId, $"{path}.optionId", errors);
        ValidateId(response.OutcomeId, $"{path}.outcomeId", errors);
        ValidateRoll(response.Roll, $"{path}.roll", errors);
        for (int i = 0; i < response.FlagChanges.Count; i++)
        {
            ValidateId(response.FlagChanges[i].FlagId, $"{path}.flagChanges[{i}].flagId", errors);
        }

        if (response.TerminalReasonCode is null)
        {
            return;
        }

        ValidateId(response.TerminalReasonCode, $"{path}.terminalReasonCode", errors);
        bool mayEndRun = week.IsFinal && isLastEvent && (state.Status is RunStatus.Bankrupt or RunStatus.SpecialEnding);
        if (!mayEndRun)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.TerminalReasonNotApplied, $"{path}.terminalReasonCode"));
        }
    }

    private static void ValidateLedger(RunState state, List<WeekView> weeks, List<RunStateError> errors)
    {
        Dictionary<int, WeekView> weeksByNumber = new();
        foreach (WeekView week in weeks)
        {
            weeksByNumber.TryAdd(week.Week, week);
        }

        Dictionary<int, int> subscriptionsByWeek = new();
        HashSet<(int Week, int Step, CashFlowSource Source, int Ordinal)> keys = new();
        for (int i = 0; i < state.Ledger.Count; i++)
        {
            CashFlowEntry entry = state.Ledger[i];
            string path = $"ledger[{i}]";
            if (!CategoryMatchesSource(entry))
            {
                errors.Add(new RunStateError(RunStateErrorCodes.LedgerCategoryMismatch, $"{path}.category"));
            }

            if (!AmountAndOrdinalValid(entry))
            {
                errors.Add(new RunStateError(RunStateErrorCodes.LedgerAmountInvalid, path));
            }

            if (!keys.Add((entry.Week, entry.Step, entry.Source, entry.Ordinal)))
            {
                errors.Add(new RunStateError(RunStateErrorCodes.LedgerDuplicateEntry, path));
            }

            if (!weeksByNumber.TryGetValue(entry.Week, out WeekView? week))
            {
                errors.Add(new RunStateError(RunStateErrorCodes.LedgerWeekOutOfRange, $"{path}.week"));
                continue;
            }

            if (!StepMatchesSource(entry, week))
            {
                errors.Add(new RunStateError(RunStateErrorCodes.LedgerStepInvalid, $"{path}.step"));
            }

            if (entry.Source == CashFlowSource.Subscription)
            {
                subscriptionsByWeek[entry.Week] = subscriptionsByWeek.GetValueOrDefault(entry.Week) + 1;
            }
        }

        foreach (WeekView week in weeks)
        {
            if (subscriptionsByWeek.GetValueOrDefault(week.Week) != 1)
            {
                errors.Add(new RunStateError(RunStateErrorCodes.SubscriptionEntryCount, week.Path));
            }
        }
    }

    private static bool CategoryMatchesSource(CashFlowEntry entry)
    {
        return entry.Source switch
        {
            CashFlowSource.WeeklyActionCost or CashFlowSource.EncounterCost or CashFlowSource.ResponseCost =>
                entry.Category == CashFlowCategory.Expenses,
            CashFlowSource.Subscription => entry.Category == CashFlowCategory.Subscriptions,
            CashFlowSource.WeeklyActionOutcome or CashFlowSource.ResponseOutcome =>
                entry.Category is CashFlowCategory.Sponsors or CashFlowCategory.Donations or CashFlowCategory.Expenses,
            _ => false,
        };
    }

    private static bool AmountAndOrdinalValid(CashFlowEntry entry)
    {
        return entry.Source switch
        {
            CashFlowSource.Subscription => entry.AmountPln >= 0 && entry.Ordinal == 0,
            CashFlowSource.WeeklyActionCost or CashFlowSource.EncounterCost or CashFlowSource.ResponseCost =>
                entry.AmountPln > 0 && entry.Ordinal == 0,
            _ => entry.AmountPln > 0 && entry.Ordinal >= 0,
        };
    }

    private static bool StepMatchesSource(CashFlowEntry entry, WeekView week)
    {
        return entry.Source switch
        {
            CashFlowSource.WeeklyActionCost or CashFlowSource.WeeklyActionOutcome or CashFlowSource.Subscription => entry.Step == 0,
            CashFlowSource.EncounterCost =>
                entry.Step >= 1 && (EventAt(week, entry.Step) is not null || week.Pending?.Index == entry.Step),
            CashFlowSource.ResponseCost or CashFlowSource.ResponseOutcome => EventAt(week, entry.Step)?.Response is not null,
            _ => false,
        };
    }

    private static EventResolution? EventAt(WeekView week, int step)
    {
        return step >= 1 && step <= week.Events.Count ? week.Events[step - 1] : null;
    }

    private static void ValidateReconciliation(RunState state, GameParameters parameters, List<WeekView> weeks, List<RunStateError> errors)
    {
        Int128 money = parameters.StartingMoneyPln;
        foreach (CashFlowEntry entry in state.Ledger)
        {
            money += entry.SignedAmountPln;
        }

        if (money != state.MoneyPln)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.MoneyNotReconciled, "moneyPln"));
        }

        long viewers = parameters.StartingViewers;
        long drama = parameters.StartingDrama;
        foreach (WeekView week in weeks)
        {
            viewers += week.Action.ViewersDelta;
            drama += week.Action.DramaDelta;
            foreach (EventResolution resolution in week.Events)
            {
                if (resolution.Response is { } response)
                {
                    viewers += response.ViewersDelta;
                    drama += response.DramaDelta;
                }
            }
        }

        if (viewers != state.Viewers)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.ViewersNotReconciled, "viewers"));
        }

        if (drama != state.Drama)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.DramaNotReconciled, "drama"));
        }
    }

    private static void ValidateFlags(RunState state, List<RunStateError> errors)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        for (int i = 0; i < state.Flags.Count; i++)
        {
            NarrativeFlag flag = state.Flags[i];
            string path = $"flags[{i}]";
            ValidateId(flag.FlagId, $"{path}.flagId", errors);
            if (!seen.Add(flag.FlagId) || flag.SetWeek < 1 || flag.SetWeek > state.Week)
            {
                errors.Add(new RunStateError(RunStateErrorCodes.FlagInvalid, path));
            }
        }
    }

    private static void ValidateId(string value, string path, List<RunStateError> errors)
    {
        if (!StableId.IsValid(value))
        {
            errors.Add(new RunStateError(RunStateErrorCodes.InvalidStableId, path));
        }
    }

    private static void ValidateRoll(int roll, string path, List<RunStateError> errors)
    {
        if (roll is < 0 or > MaxRoll)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.RollOutOfRange, path));
        }
    }

    private sealed record WeekView(
        int Week,
        string Path,
        ActionResolution Action,
        IReadOnlyList<EncounterRoll> EncounterRolls,
        IReadOnlyList<EventResolution> Events,
        string EventsPath,
        PendingEvent? Pending,
        bool IsFinal);
}
