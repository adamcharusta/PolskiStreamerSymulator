using System.Text.Json.Serialization;
using PolskiStreamerSymulatorApp.Contracts.Serialization;

namespace PolskiStreamerSymulatorApp.Contracts.Runs;

public sealed record WeekRecordDto(
    int Week,
    ActionResolutionDto Action,
    IReadOnlyList<EncounterRollDto> EncounterRolls,
    IReadOnlyList<EventResolutionDto> Events) : IJsonOnDeserialized
{
    void IJsonOnDeserialized.OnDeserialized()
    {
        NullElementGuard.ThrowIfAnyNull(EncounterRolls, "encounterRolls");
        NullElementGuard.ThrowIfAnyNull(Events, "events");
    }
}

public sealed record WeekInProgressDto(
    int Week,
    ActionResolutionDto Action,
    IReadOnlyList<EncounterRollDto> EncounterRolls,
    IReadOnlyList<EventResolutionDto> ResolvedEvents,
    PendingEventDto Pending) : IJsonOnDeserialized
{
    void IJsonOnDeserialized.OnDeserialized()
    {
        NullElementGuard.ThrowIfAnyNull(EncounterRolls, "encounterRolls");
        NullElementGuard.ThrowIfAnyNull(ResolvedEvents, "resolvedEvents");
    }
}

public sealed record ActionResolutionDto(string ActionId, int Roll, string OutcomeId, int ViewersDelta, int DramaDelta);

public sealed record EncounterRollDto(string EventId, int Roll, bool Passed);

public sealed record PendingEventDto(string EventId, int Index);

public sealed record EventResolutionDto(string EventId, int Index, ResponseResolutionDto? Response);

public sealed record ResponseResolutionDto(
    string OptionId,
    int Roll,
    string OutcomeId,
    int ViewersDelta,
    int DramaDelta,
    IReadOnlyList<FlagChangeDto> FlagChanges,
    string? TerminalReasonCode) : IJsonOnDeserialized
{
    void IJsonOnDeserialized.OnDeserialized()
    {
        NullElementGuard.ThrowIfAnyNull(FlagChanges, "flagChanges");
    }
}

public sealed record FlagChangeDto(string FlagId, FlagOperationDto Operation);

[JsonConverter(typeof(StrictEnumConverter<FlagOperationDto>))]
public enum FlagOperationDto
{
    [JsonStringEnumMemberName("set")]
    Set,

    [JsonStringEnumMemberName("clear")]
    Clear,
}
