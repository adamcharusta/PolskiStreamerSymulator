namespace PolskiStreamerSymulatorApp.Contracts.Localization;

/// <summary>
/// Locales the game ships. A locale is a query parameter for text and never part of a run state.
/// </summary>
public static class SupportedLocales
{
    public const string Polish = "pl-PL";
    public const string English = "en";
    public const string Default = Polish;

    public static IReadOnlyList<string> All { get; } = [Polish, English];

    public static bool IsSupported(string? locale)
    {
        return locale is Polish or English;
    }
}
