using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Randomness;

namespace PolskiStreamerSymulatorApp.Domain.Engine;

/// <summary>
/// Selects the weighted outcome a roll lands on. Outcomes are walked in catalogue order with a running sum of their chances,
/// and the first outcome whose running sum exceeds the roll wins, so an outcome with a chance of 0 is never selected.
/// </summary>
public static class OutcomeTable
{
    public static WeeklyActionOutcome Select(IReadOnlyList<WeeklyActionOutcome> outcomes, int roll)
    {
        ArgumentNullException.ThrowIfNull(outcomes);
        ArgumentOutOfRangeException.ThrowIfNegative(roll);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(roll, (int)Pcg32.RollBound);
        if (outcomes.Any(static outcome => outcome.ChanceBps < 0)
            || outcomes.Sum(static outcome => (long)outcome.ChanceBps) != GameCatalogValidator.TotalChanceBps)
        {
            throw new ArgumentException("Outcome chances must be non-negative and sum to 10,000 basis points.", nameof(outcomes));
        }

        int runningSum = 0;
        foreach (WeeklyActionOutcome outcome in outcomes)
        {
            runningSum += outcome.ChanceBps;
            if (roll < runningSum)
            {
                return outcome;
            }
        }

        throw new InvalidOperationException("Unreachable: the chances sum to 10,000 and the roll is below 10,000.");
    }
}
