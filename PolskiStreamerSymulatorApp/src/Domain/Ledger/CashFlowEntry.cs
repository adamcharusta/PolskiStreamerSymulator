namespace PolskiStreamerSymulatorApp.Domain.Ledger;

/// <summary>
/// One categorized money change. Week and step point to the week record that names its action, event, option, and outcome.
/// Step 0 is the weekly action step, which also carries the subscription settlement; step k is the week's k-th selected event.
/// <see cref="AmountPln"/> is a non-negative magnitude; its sign comes from the category (see <see cref="SignedAmountPln"/>).
/// </summary>
public sealed record CashFlowEntry(int Week, int Step, CashFlowSource Source, int Ordinal, CashFlowCategory Category, long AmountPln)
{
    public long SignedAmountPln => Category == CashFlowCategory.Expenses ? -AmountPln : AmountPln;
}

public enum CashFlowCategory
{
    Sponsors,
    Donations,
    Subscriptions,
    Expenses,
}

public enum CashFlowSource
{
    WeeklyActionCost,
    WeeklyActionOutcome,
    Subscription,
    EncounterCost,
    ResponseCost,
    ResponseOutcome,
}
