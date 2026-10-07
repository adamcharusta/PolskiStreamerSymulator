namespace PolskiStreamerSymulatorApp.Domain.Catalog;

/// <summary>
/// Typed numeric parameters of one published catalogue version. A run pins the row of its catalogue version.
/// </summary>
public sealed record GameParameters(
    int RunLengthWeeks,
    int MaxEventsPerWeek,
    long StartingMoneyPln,
    int StartingViewers,
    int StartingDrama,
    long BankruptcyThresholdPln,
    int SubscriptionViewersPerPln,
    int ScoreAudienceReferenceViewers,
    long ScoreProfitReferencePln,
    int ScorePointsPerComponent,
    int CalmDramaMax,
    int MiddleDramaMax);
