namespace PolskiStreamerSymulatorApp.Contracts.Problems;

/// <summary>
/// Stable error codes the API returns in Problem Details responses.
/// </summary>
public static class ProblemCodes
{
    public const string InvalidRequest = "invalid_request";
    public const string InvalidState = "invalid_state";
    public const string InvalidChoice = "invalid_choice";
    public const string InvalidStreamerName = "invalid_streamer_name";
    public const string IncompatibleSave = "incompatible_save";
    public const string CatalogVersionUnavailable = "catalog_version_unavailable";
    public const string RunFinished = "run_finished";

    public static IReadOnlyList<string> All { get; } =
    [
        InvalidRequest,
        InvalidState,
        InvalidChoice,
        InvalidStreamerName,
        IncompatibleSave,
        CatalogVersionUnavailable,
        RunFinished,
    ];
}
