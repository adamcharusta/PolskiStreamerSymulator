namespace PolskiStreamerSymulatorApp.Domain.Catalog;

/// <summary>
/// Applies the publication rules for a game parameter row. Each error is the name of the field that breaks a rule.
/// </summary>
public static class GameParametersValidator
{
    public const int MinRunLengthWeeks = 1;
    public const int MaxRunLengthWeeks = 260;
    public const int MaxDrama = 100;

    public static IReadOnlyList<string> Validate(GameParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        List<string> errors = [];

        if (parameters.RunLengthWeeks is < MinRunLengthWeeks or > MaxRunLengthWeeks)
        {
            errors.Add(nameof(GameParameters.RunLengthWeeks));
        }

        if (parameters.MaxEventsPerWeek < 1)
        {
            errors.Add(nameof(GameParameters.MaxEventsPerWeek));
        }

        if (parameters.StartingViewers < 0)
        {
            errors.Add(nameof(GameParameters.StartingViewers));
        }

        if (parameters.StartingDrama is < 0 or > MaxDrama)
        {
            errors.Add(nameof(GameParameters.StartingDrama));
        }

        if (parameters.BankruptcyThresholdPln >= 0 || parameters.BankruptcyThresholdPln >= parameters.StartingMoneyPln)
        {
            errors.Add(nameof(GameParameters.BankruptcyThresholdPln));
        }

        if (parameters.SubscriptionViewersPerPln < 1)
        {
            errors.Add(nameof(GameParameters.SubscriptionViewersPerPln));
        }

        if (parameters.ScoreAudienceReferenceViewers < 1)
        {
            errors.Add(nameof(GameParameters.ScoreAudienceReferenceViewers));
        }

        if (parameters.ScoreProfitReferencePln < 1)
        {
            errors.Add(nameof(GameParameters.ScoreProfitReferencePln));
        }

        if (parameters.ScorePointsPerComponent < 1)
        {
            errors.Add(nameof(GameParameters.ScorePointsPerComponent));
        }

        if (parameters.CalmDramaMax < 0 || parameters.CalmDramaMax >= parameters.MiddleDramaMax)
        {
            errors.Add(nameof(GameParameters.CalmDramaMax));
        }

        if (parameters.MiddleDramaMax >= MaxDrama)
        {
            errors.Add(nameof(GameParameters.MiddleDramaMax));
        }

        return errors;
    }
}
