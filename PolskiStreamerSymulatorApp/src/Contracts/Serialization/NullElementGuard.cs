using System.Text.Json;

namespace PolskiStreamerSymulatorApp.Contracts.Serialization;

/// <summary>
/// Rejects null array elements after deserialization, because nullable annotations do not cover collection elements.
/// </summary>
internal static class NullElementGuard
{
    public static void ThrowIfAnyNull<T>(IReadOnlyList<T> items, string propertyName)
        where T : class
    {
        foreach (T? item in items)
        {
            if (item is null)
            {
                throw new JsonException($"The JSON array '{propertyName}' must not contain null.");
            }
        }
    }
}
