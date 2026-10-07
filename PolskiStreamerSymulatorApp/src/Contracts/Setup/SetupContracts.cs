namespace PolskiStreamerSymulatorApp.Contracts.Setup;

/// <summary>
/// What the setup screen shows before a run starts, read from one published catalogue version.
/// </summary>
public sealed record SetupOptionsResponse(
    int CatalogVersion,
    int RunLengthWeeks,
    long StartingMoneyPln,
    int StartingViewers,
    int StartingDrama,
    long BankruptcyThresholdPln,
    string SuggestedStreamerName);

public sealed record StreamerNameSuggestionResponse(int CatalogVersion, string StreamerName);

/// <summary>
/// Starts a run on the catalogue version shown during setup. The server answers with the new RunStateDto.
/// </summary>
public sealed record StartRunRequest(int CatalogVersion, string StreamerName);
