using PolskiStreamerSymulatorApp.Domain.Ledger;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Ledger;

public sealed class CashFlowEntryTests
{
    [Theory]
    [InlineData(CashFlowCategory.Sponsors, 250L)]
    [InlineData(CashFlowCategory.Donations, 20L)]
    [InlineData(CashFlowCategory.Subscriptions, 3L)]
    [InlineData(CashFlowCategory.Expenses, -50L)]
    public void SignedAmountSubtractsOnlyExpenses(CashFlowCategory category, long expected)
    {
        CashFlowEntry entry = new(Week: 1, Step: 0, CashFlowSource.WeeklyActionOutcome, Ordinal: 0, category, Math.Abs(expected));

        Assert.Equal(expected, entry.SignedAmountPln);
    }
}
