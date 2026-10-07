using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Randomness;

namespace PolskiStreamerSymulatorApp.Domain.Runs;

/// <summary>
/// One career as saved in the browser. Untrusted until RunStateValidator turns it into a ValidatedRunState.
/// </summary>
public sealed record RunState(
    int RulesVersion,
    int CatalogVersion,
    Guid RunId,
    string StreamerName,
    int Week,
    RunStatus Status,
    long MoneyPln,
    int Viewers,
    int Drama,
    RngState Rng,
    IReadOnlyList<CashFlowEntry> Ledger,
    IReadOnlyList<WeekRecord> History,
    WeekInProgress? CurrentWeek,
    IReadOnlyList<NarrativeFlag> Flags,
    RunEnding? Ending);

public enum RunStatus
{
    Active,
    PendingWeek,
    Completed,
    Bankrupt,
    SpecialEnding,
}

/// <summary>
/// A browser-local story flag and the week a rolled result set it.
/// </summary>
public sealed record NarrativeFlag(string FlagId, int SetWeek);

/// <summary>
/// How and when a run ended. Only a special ending carries a reason code.
/// </summary>
public sealed record RunEnding(RunEndingKind Kind, int Week, string? ReasonCode);

public enum RunEndingKind
{
    Completed,
    Bankrupt,
    Special,
}
