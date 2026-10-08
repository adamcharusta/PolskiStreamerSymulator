namespace PolskiStreamerSymulatorApp.Domain.Engine;

/// <summary>
/// Stable codes for a week that cannot be planned. The S1 design spec states each condition and the problem code it maps to.
/// </summary>
public static class WeekPlanErrorCodes
{
    public const string CatalogMismatch = "catalog_mismatch";
    public const string WeekPending = "week_pending";
    public const string RunFinished = "run_finished";
    public const string UnknownAction = "unknown_action";
    public const string ValueOutOfRange = "value_out_of_range";

    public static IReadOnlyList<string> All { get; } =
    [
        CatalogMismatch,
        WeekPending,
        RunFinished,
        UnknownAction,
        ValueOutOfRange,
    ];
}
