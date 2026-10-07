using System.Text.RegularExpressions;

namespace PolskiStreamerSymulatorApp.Domain.Identity;

/// <summary>
/// Checks the stable IDs shared by catalogue content, saves, and localized text lookups: lowercase snake case, 1 to 64 characters.
/// </summary>
public static partial class StableId
{
    public static bool IsValid(string? value)
    {
        return value is not null && Pattern().IsMatch(value);
    }

    [GeneratedRegex(@"^[a-z][a-z0-9_]{0,63}\z", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
