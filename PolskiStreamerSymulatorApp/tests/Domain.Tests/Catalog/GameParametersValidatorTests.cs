using PolskiStreamerSymulatorApp.Domain.Catalog;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Catalog;

public sealed class GameParametersValidatorTests
{
    private static readonly GameParameters FirstPublished = new(
        RunLengthWeeks: 52,
        MaxEventsPerWeek: 3,
        StartingMoneyPln: 1_500,
        StartingViewers: 20,
        StartingDrama: 50,
        BankruptcyThresholdPln: -1_000,
        SubscriptionViewersPerPln: 10,
        ScoreAudienceReferenceViewers: 1_000,
        ScoreProfitReferencePln: 5_000,
        ScorePointsPerComponent: 500,
        CalmDramaMax: 33,
        MiddleDramaMax: 66);

    private static readonly Dictionary<string, (GameParameters Parameters, string Field)> Violations = new()
    {
        ["run length 0"] = (FirstPublished with { RunLengthWeeks = 0 }, nameof(GameParameters.RunLengthWeeks)),
        ["run length 261"] = (FirstPublished with { RunLengthWeeks = 261 }, nameof(GameParameters.RunLengthWeeks)),
        ["event cap 0"] = (FirstPublished with { MaxEventsPerWeek = 0 }, nameof(GameParameters.MaxEventsPerWeek)),
        ["starting viewers -1"] = (FirstPublished with { StartingViewers = -1 }, nameof(GameParameters.StartingViewers)),
        ["starting drama -1"] = (FirstPublished with { StartingDrama = -1 }, nameof(GameParameters.StartingDrama)),
        ["starting drama 101"] = (FirstPublished with { StartingDrama = 101 }, nameof(GameParameters.StartingDrama)),
        ["threshold 0"] = (FirstPublished with { BankruptcyThresholdPln = 0 }, nameof(GameParameters.BankruptcyThresholdPln)),
        ["threshold equal to starting money"] =
            (FirstPublished with { StartingMoneyPln = -1_000 }, nameof(GameParameters.BankruptcyThresholdPln)),
        ["subscription divisor 0"] =
            (FirstPublished with { SubscriptionViewersPerPln = 0 }, nameof(GameParameters.SubscriptionViewersPerPln)),
        ["audience reference 0"] =
            (FirstPublished with { ScoreAudienceReferenceViewers = 0 }, nameof(GameParameters.ScoreAudienceReferenceViewers)),
        ["profit reference 0"] = (FirstPublished with { ScoreProfitReferencePln = 0 }, nameof(GameParameters.ScoreProfitReferencePln)),
        ["points per component 0"] =
            (FirstPublished with { ScorePointsPerComponent = 0 }, nameof(GameParameters.ScorePointsPerComponent)),
        ["calm maximum -1"] = (FirstPublished with { CalmDramaMax = -1 }, nameof(GameParameters.CalmDramaMax)),
        ["calm maximum equal to middle"] = (FirstPublished with { CalmDramaMax = 66 }, nameof(GameParameters.CalmDramaMax)),
        ["middle maximum 100"] = (FirstPublished with { MiddleDramaMax = 100 }, nameof(GameParameters.MiddleDramaMax)),
    };

    public static TheoryData<string> ViolationNames => new(Violations.Keys);

    [Fact]
    public void FirstPublishedValuesPass()
    {
        Assert.Empty(GameParametersValidator.Validate(FirstPublished));
    }

    [Fact]
    public void BoundaryValuesInsideTheRulesPass()
    {
        GameParameters[] valid =
        [
            FirstPublished with { RunLengthWeeks = 1 },
            FirstPublished with { RunLengthWeeks = 260 },
            FirstPublished with { StartingViewers = 0 },
            FirstPublished with { StartingDrama = 0 },
            FirstPublished with { StartingDrama = 100 },
            FirstPublished with { CalmDramaMax = 0 },
            FirstPublished with { MiddleDramaMax = 99 },
            FirstPublished with { BankruptcyThresholdPln = -1, StartingMoneyPln = 0 },
        ];

        Assert.All(valid, parameters => Assert.Empty(GameParametersValidator.Validate(parameters)));
    }

    [Theory]
    [MemberData(nameof(ViolationNames))]
    public void ViolationNamesOnlyTheBrokenField(string name)
    {
        (GameParameters parameters, string field) = Violations[name];

        IReadOnlyList<string> errors = GameParametersValidator.Validate(parameters);

        Assert.Equal(new[] { field }, errors);
    }
}
