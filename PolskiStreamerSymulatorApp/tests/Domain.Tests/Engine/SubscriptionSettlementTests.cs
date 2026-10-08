using PolskiStreamerSymulatorApp.Domain.Engine;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Engine;

public sealed class SubscriptionSettlementTests
{
    [Theory]
    [InlineData(0, 0L)]
    [InlineData(9, 0L)]
    [InlineData(20, 2L)]
    [InlineData(100, 10L)]
    [InlineData(250, 25L)]
    [InlineData(1_000, 100L)]
    [InlineData(-5, 0L)]
    public void PaysOnePlnPerTenViewersUnderTheFirstDivisor(int viewers, long expectedPln)
    {
        Assert.Equal(expectedPln, SubscriptionSettlement.WeeklyAmountPln(viewers, subscriptionViewersPerPln: 10));
    }

    [Fact]
    public void DivisorOfOnePaysOnePlnPerViewer()
    {
        Assert.Equal(37L, SubscriptionSettlement.WeeklyAmountPln(37, subscriptionViewersPerPln: 1));
    }

    [Fact]
    public void DivisorBelowOneIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SubscriptionSettlement.WeeklyAmountPln(20, subscriptionViewersPerPln: 0));
    }
}
