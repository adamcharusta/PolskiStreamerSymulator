namespace PolskiStreamerSymulatorApp.Domain.Runs;

/// <summary>
/// Stable codes for run-state validation failures. The F2 design spec states the rule behind each code.
/// </summary>
public static class RunStateErrorCodes
{
    public const string InvalidGameParameters = "invalid_game_parameters";
    public const string UnsupportedRulesVersion = "unsupported_rules_version";
    public const string InvalidCatalogVersion = "invalid_catalog_version";
    public const string InvalidRunId = "invalid_run_id";
    public const string InvalidStreamerName = "invalid_streamer_name";
    public const string UnsupportedRngAlgorithm = "unsupported_rng_algorithm";
    public const string WeekOutOfRange = "week_out_of_range";
    public const string ViewersNegative = "viewers_negative";
    public const string DramaOutOfRange = "drama_out_of_range";
    public const string InvalidStableId = "invalid_stable_id";
    public const string StatusInconsistent = "status_inconsistent";
    public const string EndingInconsistent = "ending_inconsistent";
    public const string MoneyAtOrBelowBankruptcyThreshold = "money_at_or_below_bankruptcy_threshold";
    public const string BankruptMoneyAboveThreshold = "bankrupt_money_above_threshold";
    public const string HistoryInconsistent = "history_inconsistent";
    public const string EventCapExceeded = "event_cap_exceeded";
    public const string EventIndexInconsistent = "event_index_inconsistent";
    public const string EventSelectedTwice = "event_selected_twice";
    public const string EncounterRolledTwice = "encounter_rolled_twice";
    public const string SelectedEventWithoutPassingRoll = "selected_event_without_passing_roll";
    public const string UnansweredEvent = "unanswered_event";
    public const string TerminalReasonNotApplied = "terminal_reason_not_applied";
    public const string RollOutOfRange = "roll_out_of_range";
    public const string LedgerWeekOutOfRange = "ledger_week_out_of_range";
    public const string LedgerStepInvalid = "ledger_step_invalid";
    public const string LedgerCategoryMismatch = "ledger_category_mismatch";
    public const string LedgerAmountInvalid = "ledger_amount_invalid";
    public const string LedgerDuplicateEntry = "ledger_duplicate_entry";
    public const string SubscriptionEntryCount = "subscription_entry_count";
    public const string MoneyNotReconciled = "money_not_reconciled";
    public const string ViewersNotReconciled = "viewers_not_reconciled";
    public const string DramaNotReconciled = "drama_not_reconciled";
    public const string FlagInvalid = "flag_invalid";

    public static IReadOnlyList<string> All { get; } =
    [
        InvalidGameParameters,
        UnsupportedRulesVersion,
        InvalidCatalogVersion,
        InvalidRunId,
        InvalidStreamerName,
        UnsupportedRngAlgorithm,
        WeekOutOfRange,
        ViewersNegative,
        DramaOutOfRange,
        InvalidStableId,
        StatusInconsistent,
        EndingInconsistent,
        MoneyAtOrBelowBankruptcyThreshold,
        BankruptMoneyAboveThreshold,
        HistoryInconsistent,
        EventCapExceeded,
        EventIndexInconsistent,
        EventSelectedTwice,
        EncounterRolledTwice,
        SelectedEventWithoutPassingRoll,
        UnansweredEvent,
        TerminalReasonNotApplied,
        RollOutOfRange,
        LedgerWeekOutOfRange,
        LedgerStepInvalid,
        LedgerCategoryMismatch,
        LedgerAmountInvalid,
        LedgerDuplicateEntry,
        SubscriptionEntryCount,
        MoneyNotReconciled,
        ViewersNotReconciled,
        DramaNotReconciled,
        FlagInvalid,
    ];
}
