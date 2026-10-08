namespace PolskiStreamerSymulatorApp.Domain.Catalog;

/// <summary>
/// Stable codes for catalogue validation failures. The S1 design spec states the rule behind each code.
/// </summary>
public static class CatalogErrorCodes
{
    public const string InvalidCatalogVersion = "invalid_catalog_version";
    public const string InvalidGameParameters = "invalid_game_parameters";
    public const string NoWeeklyActions = "no_weekly_actions";
    public const string InvalidStableId = "invalid_stable_id";
    public const string DuplicateActionId = "duplicate_action_id";
    public const string DuplicateOutcomeId = "duplicate_outcome_id";
    public const string CostOutOfRange = "cost_out_of_range";
    public const string NoOutcomes = "no_outcomes";
    public const string ChanceOutOfRange = "chance_out_of_range";
    public const string ChancesDoNotSum = "chances_do_not_sum";
    public const string InvalidCashFlowCategory = "invalid_cash_flow_category";
    public const string CashFlowAmountOutOfRange = "cash_flow_amount_out_of_range";
    public const string ViewersDeltaOutOfRange = "viewers_delta_out_of_range";
    public const string DramaDeltaOutOfRange = "drama_delta_out_of_range";

    public static IReadOnlyList<string> All { get; } =
    [
        InvalidCatalogVersion,
        InvalidGameParameters,
        NoWeeklyActions,
        InvalidStableId,
        DuplicateActionId,
        DuplicateOutcomeId,
        CostOutOfRange,
        NoOutcomes,
        ChanceOutOfRange,
        ChancesDoNotSum,
        InvalidCashFlowCategory,
        CashFlowAmountOutOfRange,
        ViewersDeltaOutOfRange,
        DramaDeltaOutOfRange,
    ];
}
