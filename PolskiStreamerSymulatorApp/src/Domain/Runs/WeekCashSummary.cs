using PolskiStreamerSymulatorApp.Domain.Ledger;

namespace PolskiStreamerSymulatorApp.Domain.Runs;

/// <summary>
/// The money view of one week for its report: opening money, the four category totals as non-negative amounts, and closing money,
/// where closing = opening + sponsors + donations + subscriptions - expenses. For a pending week it covers the week so far.
/// </summary>
public sealed record WeekCashSummary(
    int Week,
    long OpeningMoneyPln,
    long SponsorsPln,
    long DonationsPln,
    long SubscriptionsPln,
    long ExpensesPln,
    long ClosingMoneyPln)
{
    /// <summary>
    /// Sums in 128-bit arithmetic and throws OverflowException, rather than wrapping, when an edited save's totals do not fit in 64 bits.
    /// </summary>
    public static WeekCashSummary For(ValidatedRunState run, int week)
    {
        ArgumentNullException.ThrowIfNull(run);
        RunState state = run.State;
        bool hasRecord = state.History.Any(record => record.Week == week) || state.CurrentWeek?.Week == week;
        if (!hasRecord)
        {
            throw new ArgumentOutOfRangeException(nameof(week), week, "The run has no record for this week.");
        }

        Int128 opening = run.Parameters.StartingMoneyPln;
        Int128 sponsors = 0;
        Int128 donations = 0;
        Int128 subscriptions = 0;
        Int128 expenses = 0;
        foreach (CashFlowEntry entry in state.Ledger)
        {
            if (entry.Week < week)
            {
                opening += entry.SignedAmountPln;
            }
            else if (entry.Week == week)
            {
                switch (entry.Category)
                {
                    case CashFlowCategory.Sponsors:
                        sponsors += entry.AmountPln;
                        break;
                    case CashFlowCategory.Donations:
                        donations += entry.AmountPln;
                        break;
                    case CashFlowCategory.Subscriptions:
                        subscriptions += entry.AmountPln;
                        break;
                    case CashFlowCategory.Expenses:
                        expenses += entry.AmountPln;
                        break;
                }
            }
        }

        Int128 closing = opening + sponsors + donations + subscriptions - expenses;
        return new WeekCashSummary(
            week,
            checked((long)opening),
            checked((long)sponsors),
            checked((long)donations),
            checked((long)subscriptions),
            checked((long)expenses),
            checked((long)closing));
    }
}
