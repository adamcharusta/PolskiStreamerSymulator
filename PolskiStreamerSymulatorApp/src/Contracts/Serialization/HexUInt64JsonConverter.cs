using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolskiStreamerSymulatorApp.Contracts.Serialization;

/// <summary>
/// Writes 64-bit generator values as exactly 16 lowercase hexadecimal digits, because JSON numbers above 2^53 lose precision in JavaScript.
/// </summary>
public sealed class HexUInt64JsonConverter : JsonConverter<ulong>
{
    private const int Digits = 16;

    public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? text = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
        if (text is null
            || text.Length != Digits
            || !text.All(IsLowercaseHexDigit)
            || !ulong.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ulong value))
        {
            throw new JsonException($"Expected {Digits} lowercase hexadecimal digits.");
        }

        return value;
    }

    public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.ToString("x16", CultureInfo.InvariantCulture));
    }

    private static bool IsLowercaseHexDigit(char character)
    {
        return character is (>= '0' and <= '9') or (>= 'a' and <= 'f');
    }
}
