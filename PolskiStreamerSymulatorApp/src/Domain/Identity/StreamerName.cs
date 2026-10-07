using System.Globalization;
using System.Text;

namespace PolskiStreamerSymulatorApp.Domain.Identity;

/// <summary>
/// Normalizes and validates the streamer name a player accepts or types at setup.
/// </summary>
public static class StreamerName
{
    public const int MinDisplayedCharacters = 2;
    public const int MaxDisplayedCharacters = 32;
    public const int MaxUtf16Length = 64;

    /// <summary>
    /// Applies NFC and trims outer whitespace, then checks length and characters. Returns false and an empty string for an invalid name.
    /// </summary>
    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (input is null)
        {
            return false;
        }

        string candidate;
        try
        {
            candidate = input.Normalize(NormalizationForm.FormC).Trim();
        }
        catch (ArgumentException)
        {
            // string.Normalize rejects text that is not valid UTF-16, such as an unpaired surrogate or U+FFFE.
            return false;
        }

        if (candidate.Length > MaxUtf16Length || !HasOnlyAllowedCharacters(candidate))
        {
            return false;
        }

        int displayed = new StringInfo(candidate).LengthInTextElements;
        if (displayed is < MinDisplayedCharacters or > MaxDisplayedCharacters)
        {
            return false;
        }

        normalized = candidate;
        return true;
    }

    /// <summary>
    /// True when the value is valid and already in the form TryNormalize returns, as a saved name must be.
    /// </summary>
    public static bool IsNormalizedValid(string? value)
    {
        return TryNormalize(value, out string normalized) && string.Equals(normalized, value, StringComparison.Ordinal);
    }

    private static bool HasOnlyAllowedCharacters(string candidate)
    {
        bool hasLetterOrDigit = false;
        bool previousIsSpace = false;
        bool previousIsBase = false;
        foreach (Rune rune in candidate.EnumerateRunes())
        {
            UnicodeCategory category = Rune.GetUnicodeCategory(rune);
            if (Rune.IsLetter(rune) || category == UnicodeCategory.DecimalDigitNumber)
            {
                hasLetterOrDigit = true;
                previousIsBase = true;
                previousIsSpace = false;
            }
            else if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark)
            {
                if (!previousIsBase)
                {
                    return false;
                }

                previousIsSpace = false;
            }
            else if (rune.Value is '-' or '_')
            {
                previousIsBase = false;
                previousIsSpace = false;
            }
            else if (rune.Value == ' ' && !previousIsSpace)
            {
                previousIsBase = false;
                previousIsSpace = true;
            }
            else
            {
                return false;
            }
        }

        return hasLetterOrDigit;
    }
}
