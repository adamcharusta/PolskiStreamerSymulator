namespace PolskiStreamerSymulatorApp.Domain.Runs;

/// <summary>
/// One finished week: its weekly action, every encounter roll, and every selected event in selection order.
/// </summary>
public sealed record WeekRecord(
    int Week,
    ActionResolution Action,
    IReadOnlyList<EncounterRoll> EncounterRolls,
    IReadOnlyList<EventResolution> Events);

/// <summary>
/// The current week while a selected event waits for the player's response.
/// </summary>
public sealed record WeekInProgress(
    int Week,
    ActionResolution Action,
    IReadOnlyList<EncounterRoll> EncounterRolls,
    IReadOnlyList<EventResolution> ResolvedEvents,
    PendingEvent Pending);

/// <summary>
/// The rolled weekly action. Deltas are the changes actually applied after viewers and drama were clamped.
/// </summary>
public sealed record ActionResolution(string ActionId, int Roll, string OutcomeId, int ViewersDelta, int DramaDelta);

/// <summary>
/// The single encounter roll an event received in a week, and whether it passed its occurrence chance.
/// </summary>
public sealed record EncounterRoll(string EventId, int Roll, bool Passed);

/// <summary>
/// The selected event waiting for a response; its index is its position among the week's selected events.
/// </summary>
public sealed record PendingEvent(string EventId, int Index);

/// <summary>
/// A selected event. The response is null only when the event's encounter cost bankrupted the run.
/// </summary>
public sealed record EventResolution(string EventId, int Index, ResponseResolution? Response);

/// <summary>
/// The chosen response and its rolled outcome. A terminal reason code marks a special early ending.
/// </summary>
public sealed record ResponseResolution(
    string OptionId,
    int Roll,
    string OutcomeId,
    int ViewersDelta,
    int DramaDelta,
    IReadOnlyList<FlagChange> FlagChanges,
    string? TerminalReasonCode);

public sealed record FlagChange(string FlagId, FlagOperation Operation);

public enum FlagOperation
{
    Set,
    Clear,
}
