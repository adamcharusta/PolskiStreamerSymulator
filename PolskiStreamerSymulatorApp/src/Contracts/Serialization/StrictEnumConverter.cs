using System.Text.Json.Serialization;

namespace PolskiStreamerSymulatorApp.Contracts.Serialization;

/// <summary>
/// Reads and writes an enum only by the fixed wire name each member declares; numbers and unknown names are rejected.
/// </summary>
public sealed class StrictEnumConverter<TEnum>() : JsonStringEnumConverter<TEnum>(namingPolicy: null, allowIntegerValues: false)
    where TEnum : struct, Enum;
