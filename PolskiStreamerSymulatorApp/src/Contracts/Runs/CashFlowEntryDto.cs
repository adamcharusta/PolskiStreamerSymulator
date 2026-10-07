using System.Text.Json.Serialization;
using PolskiStreamerSymulatorApp.Contracts.Serialization;

namespace PolskiStreamerSymulatorApp.Contracts.Runs;

public sealed record CashFlowEntryDto(
    int Week,
    int Step,
    CashFlowSourceDto Source,
    int Ordinal,
    CashFlowCategoryDto Category,
    long AmountPln);

[JsonConverter(typeof(StrictEnumConverter<CashFlowSourceDto>))]
public enum CashFlowSourceDto
{
    [JsonStringEnumMemberName("weeklyActionCost")]
    WeeklyActionCost,

    [JsonStringEnumMemberName("weeklyActionOutcome")]
    WeeklyActionOutcome,

    [JsonStringEnumMemberName("subscription")]
    Subscription,

    [JsonStringEnumMemberName("encounterCost")]
    EncounterCost,

    [JsonStringEnumMemberName("responseCost")]
    ResponseCost,

    [JsonStringEnumMemberName("responseOutcome")]
    ResponseOutcome,
}

[JsonConverter(typeof(StrictEnumConverter<CashFlowCategoryDto>))]
public enum CashFlowCategoryDto
{
    [JsonStringEnumMemberName("sponsors")]
    Sponsors,

    [JsonStringEnumMemberName("donations")]
    Donations,

    [JsonStringEnumMemberName("subscriptions")]
    Subscriptions,

    [JsonStringEnumMemberName("expenses")]
    Expenses,
}
