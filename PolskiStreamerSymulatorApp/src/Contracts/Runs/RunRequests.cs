namespace PolskiStreamerSymulatorApp.Contracts.Runs;

/// <summary>
/// Plays the weekly action of the current week. The server answers with the resulting RunStateDto.
/// </summary>
public sealed record PlanWeekRequest(RunStateDto State, string ActionId);

/// <summary>
/// Answers the pending event. The server answers with the resulting RunStateDto.
/// </summary>
public sealed record ChooseEventOptionRequest(RunStateDto State, string OptionId);

/// <summary>
/// Version of the JSON shape of saves and run payloads. Any shape change raises it.
/// </summary>
public static class SaveSchema
{
    public const int CurrentVersion = 1;
}
