using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolskiStreamerSymulatorApp.Contracts.Serialization;

/// <summary>
/// Reads and writes an enum only by the fixed wire name each member declares. Only an exact, case-sensitive string
/// match is accepted: numbers, unknown names, comma lists, padded names, and undefined values are rejected.
/// </summary>
public sealed class StrictEnumConverter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private static readonly Dictionary<string, TEnum> ValuesByName = [];
    private static readonly Dictionary<TEnum, string> NamesByValue = [];

    static StrictEnumConverter()
    {
        foreach (FieldInfo field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            TEnum value = (TEnum)field.GetValue(null)!;
            string name = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name ?? field.Name;
            ValuesByName[name] = value;
            NamesByValue[value] = name;
        }
    }

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected a string for {typeof(TEnum).Name}.");
        }

        string? name = reader.GetString();
        if (name is null || !ValuesByName.TryGetValue(name, out TEnum value))
        {
            throw new JsonException($"'{name}' is not a valid {typeof(TEnum).Name} name.");
        }

        return value;
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        if (!NamesByValue.TryGetValue(value, out string? name))
        {
            throw new JsonException($"{value} is not a defined {typeof(TEnum).Name} member.");
        }

        writer.WriteStringValue(name);
    }
}
