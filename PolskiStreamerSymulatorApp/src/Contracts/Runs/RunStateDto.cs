using System.Text.Json.Serialization;
using PolskiStreamerSymulatorApp.Contracts.Serialization;

namespace PolskiStreamerSymulatorApp.Contracts.Runs;

/// <summary>
/// A career as saved in the browser and exchanged with the server: the domain run state plus the save schema version.
/// </summary>
public sealed record RunStateDto(
    int SchemaVersion,
    int RulesVersion,
    int CatalogVersion,
    Guid RunId,
    string StreamerName,
    int Week,
    RunStatusDto Status,
    long MoneyPln,
    int Viewers,
    int Drama,
    RngStateDto Rng,
    IReadOnlyList<CashFlowEntryDto> Ledger,
    IReadOnlyList<WeekRecordDto> History,
    WeekInProgressDto? CurrentWeek,
    IReadOnlyList<NarrativeFlagDto> Flags,
    RunEndingDto? Ending) : IJsonOnDeserialized
{
    void IJsonOnDeserialized.OnDeserialized()
    {
        NullElementGuard.ThrowIfAnyNull(Ledger, "ledger");
        NullElementGuard.ThrowIfAnyNull(History, "history");
        NullElementGuard.ThrowIfAnyNull(Flags, "flags");
    }
}

[JsonConverter(typeof(StrictEnumConverter<RunStatusDto>))]
public enum RunStatusDto
{
    [JsonStringEnumMemberName("active")]
    Active,

    [JsonStringEnumMemberName("pendingWeek")]
    PendingWeek,

    [JsonStringEnumMemberName("completed")]
    Completed,

    [JsonStringEnumMemberName("bankrupt")]
    Bankrupt,

    [JsonStringEnumMemberName("specialEnding")]
    SpecialEnding,
}

/// <summary>
/// The saved random generator position. The three numbers travel as 16-digit hexadecimal strings.
/// </summary>
public sealed record RngStateDto(
    string Algorithm,
    [property: JsonConverter(typeof(HexUInt64JsonConverter))] ulong Seed,
    [property: JsonConverter(typeof(HexUInt64JsonConverter))] ulong Stream,
    [property: JsonConverter(typeof(HexUInt64JsonConverter))] ulong State);

public sealed record NarrativeFlagDto(string FlagId, int SetWeek);

public sealed record RunEndingDto(RunEndingKindDto Kind, int Week, string? ReasonCode);

[JsonConverter(typeof(StrictEnumConverter<RunEndingKindDto>))]
public enum RunEndingKindDto
{
    [JsonStringEnumMemberName("completed")]
    Completed,

    [JsonStringEnumMemberName("bankrupt")]
    Bankrupt,

    [JsonStringEnumMemberName("special")]
    Special,
}
