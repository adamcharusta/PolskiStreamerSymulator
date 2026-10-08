namespace PolskiStreamerSymulatorApp.Domain.Engine;

/// <summary>
/// The weekly subscription formula of rules version 1: one PLN per full divisor of post-action viewers.
/// The divisor comes from the run's pinned parameters.
/// </summary>
public static class SubscriptionSettlement
{
    public static long WeeklyAmountPln(int viewers, int subscriptionViewersPerPln)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(subscriptionViewersPerPln, 1);
        return Math.Max(0, viewers) / subscriptionViewersPerPln;
    }
}
