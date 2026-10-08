using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Tests.Runs;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Catalog;

/// <summary>
/// The four working weekly actions from balance.md as catalogue version 1. Test data only; S2 moves the content to SQLite.
/// </summary>
internal static class DraftCatalog
{
    public static IReadOnlyList<WeeklyActionDefinition> WeeklyActions { get; } =
    [
        new WeeklyActionDefinition(
            "regular_stream",
            GuaranteedCostPln: 0,
            [
                Outcome("steady", 6_000, viewersDelta: 8, dramaDelta: 0),
                Outcome("good_chat", 3_000, viewersDelta: 15, dramaDelta: 1, Donations(20)),
                Outcome("slow_evening", 1_000, viewersDelta: 0, dramaDelta: -1),
            ]),
        new WeeklyActionDefinition(
            "provocative_stunt",
            GuaranteedCostPln: 100,
            [
                Outcome("viral", 4_500, viewersDelta: 45, dramaDelta: 12, Donations(80)),
                Outcome("mixed_reaction", 3_500, viewersDelta: 15, dramaDelta: 8, Donations(20)),
                Outcome("backlash", 2_000, viewersDelta: -10, dramaDelta: 20, Expense(150)),
            ]),
        new WeeklyActionDefinition(
            "sponsor_pitch",
            GuaranteedCostPln: 50,
            [
                Outcome("deal", 5_000, viewersDelta: 5, dramaDelta: 0, Sponsors(250)),
                Outcome("small_deal", 3_000, viewersDelta: 0, dramaDelta: 1, Sponsors(100)),
                Outcome("rejected", 2_000, viewersDelta: -3, dramaDelta: 2, Expense(20)),
            ]),
        new WeeklyActionDefinition(
            "quiet_week",
            GuaranteedCostPln: 0,
            [
                Outcome("rest", 7_000, viewersDelta: -1, dramaDelta: -5),
                Outcome("loyal_audience", 3_000, viewersDelta: 2, dramaDelta: -3, Donations(10)),
            ]),
    ];

    public static GameCatalog Catalog(GameParameters? parameters = null)
    {
        return new GameCatalog(Version: 1, parameters ?? RunStateFixtures.Parameters, WeeklyActions);
    }

    public static ValidatedCatalog Validated(GameParameters? parameters = null)
    {
        return Validate(Catalog(parameters));
    }

    /// <summary>A catalogue whose only action always lands on its only outcome, so a test controls the step exactly.</summary>
    public static ValidatedCatalog SingleAction(
        GameParameters parameters, long costPln, int viewersDelta, int dramaDelta, params CashFlowAmount[] cashFlows)
    {
        WeeklyActionDefinition action = new(
            "test_action", costPln, [Outcome("only_outcome", 10_000, viewersDelta, dramaDelta, cashFlows)]);
        return Validate(new GameCatalog(Version: 1, parameters, [action]));
    }

    public static WeeklyActionOutcome Outcome(string outcomeId, int chanceBps, int viewersDelta, int dramaDelta, params CashFlowAmount[] cashFlows)
    {
        return new WeeklyActionOutcome(outcomeId, chanceBps, viewersDelta, dramaDelta, cashFlows);
    }

    public static CashFlowAmount Sponsors(long amountPln)
    {
        return new CashFlowAmount(CashFlowCategory.Sponsors, amountPln);
    }

    public static CashFlowAmount Donations(long amountPln)
    {
        return new CashFlowAmount(CashFlowCategory.Donations, amountPln);
    }

    public static CashFlowAmount Expense(long amountPln)
    {
        return new CashFlowAmount(CashFlowCategory.Expenses, amountPln);
    }

    private static ValidatedCatalog Validate(GameCatalog catalog)
    {
        CatalogValidation validation = GameCatalogValidator.Validate(catalog);
        Assert.True(validation.IsValid, string.Join(", ", validation.Errors));
        return validation.Value;
    }
}
