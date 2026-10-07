using System.Text.Json.Serialization;
using PolskiStreamerSymulatorApp.Contracts.Runs;
using PolskiStreamerSymulatorApp.Contracts.Setup;

namespace PolskiStreamerSymulatorApp.Contracts.Serialization;

/// <summary>
/// The single JSON format of the contracts: camelCase names, every property required, nulls only where declared,
/// unknown and duplicate properties rejected. Enum names and hexadecimal numbers come from converters declared on the types.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    AllowDuplicateProperties = false)]
[JsonSerializable(typeof(RunStateDto))]
[JsonSerializable(typeof(PlanWeekRequest))]
[JsonSerializable(typeof(ChooseEventOptionRequest))]
[JsonSerializable(typeof(StartRunRequest))]
[JsonSerializable(typeof(SetupOptionsResponse))]
[JsonSerializable(typeof(StreamerNameSuggestionResponse))]
[JsonSerializable(typeof(RunStatusDto))]
[JsonSerializable(typeof(RunEndingKindDto))]
[JsonSerializable(typeof(FlagOperationDto))]
[JsonSerializable(typeof(CashFlowSourceDto))]
[JsonSerializable(typeof(CashFlowCategoryDto))]
public sealed partial class ContractsJsonContext : JsonSerializerContext;
