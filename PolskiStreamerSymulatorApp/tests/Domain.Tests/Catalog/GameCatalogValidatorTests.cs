using System.Reflection;
using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Tests.Runs;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Catalog;

public sealed class GameCatalogValidatorTests
{
    private const long MaxAmount = GameCatalogValidator.MaxAmountPln;
    private const int MaxViewers = GameCatalogValidator.MaxViewersDelta;

    private static readonly Dictionary<string, (GameCatalog Catalog, CatalogError[] Expected)> Violations = new()
    {
        ["version 0"] = (DraftCatalog.Catalog() with { Version = 0 }, [Error(CatalogErrorCodes.InvalidCatalogVersion, "version")]),
        ["invalid parameters"] = (
            DraftCatalog.Catalog(RunStateFixtures.Parameters with { RunLengthWeeks = 0 }),
            [Error(CatalogErrorCodes.InvalidGameParameters, "parameters")]),
        ["no actions"] = (DraftCatalog.Catalog() with { WeeklyActions = [] }, [Error(CatalogErrorCodes.NoWeeklyActions, "weeklyActions")]),
        ["unstable action ID"] = (
            WithAction(0, static action => action with { ActionId = "Regular-Stream" }),
            [Error(CatalogErrorCodes.InvalidStableId, "weeklyActions[0].actionId")]),
        ["unstable outcome ID"] = (
            WithOutcome(0, 0, static outcome => outcome with { OutcomeId = "Steady" }),
            [Error(CatalogErrorCodes.InvalidStableId, "weeklyActions[0].outcomes[0].outcomeId")]),
        ["duplicate action ID"] = (
            WithAction(1, static action => action with { ActionId = "regular_stream" }),
            [Error(CatalogErrorCodes.DuplicateActionId, "weeklyActions[1].actionId")]),
        ["duplicate outcome ID"] = (
            WithOutcome(0, 1, static outcome => outcome with { OutcomeId = "steady" }),
            [Error(CatalogErrorCodes.DuplicateOutcomeId, "weeklyActions[0].outcomes[1].outcomeId")]),
        ["cost -1"] = (
            WithAction(0, static action => action with { GuaranteedCostPln = -1 }),
            [Error(CatalogErrorCodes.CostOutOfRange, "weeklyActions[0].guaranteedCostPln")]),
        ["cost above the maximum"] = (
            WithAction(0, static action => action with { GuaranteedCostPln = MaxAmount + 1 }),
            [Error(CatalogErrorCodes.CostOutOfRange, "weeklyActions[0].guaranteedCostPln")]),
        ["no outcomes"] = (
            WithAction(0, static action => action with { Outcomes = [] }),
            [Error(CatalogErrorCodes.NoOutcomes, "weeklyActions[0].outcomes")]),
        ["chances sum to 9,999"] = (
            WithOutcome(0, 2, static outcome => outcome with { ChanceBps = 999 }),
            [Error(CatalogErrorCodes.ChancesDoNotSum, "weeklyActions[0].outcomes")]),
        ["chances sum to 10,001"] = (
            WithOutcome(0, 2, static outcome => outcome with { ChanceBps = 1_001 }),
            [Error(CatalogErrorCodes.ChancesDoNotSum, "weeklyActions[0].outcomes")]),
        ["chance -1"] = (
            WithOutcomes(0, static outcomes => [outcomes[0] with { ChanceBps = -1 }, outcomes[1] with { ChanceBps = 9_001 }, outcomes[2]]),
            [Error(CatalogErrorCodes.ChanceOutOfRange, "weeklyActions[0].outcomes[0].chanceBps")]),
        ["chance 10,001"] = (
            WithOutcomes(0, static outcomes => [outcomes[0] with { ChanceBps = 10_001 }, outcomes[1] with { ChanceBps = -1_001 }, outcomes[2]]),
            [
                Error(CatalogErrorCodes.ChanceOutOfRange, "weeklyActions[0].outcomes[0].chanceBps"),
                Error(CatalogErrorCodes.ChanceOutOfRange, "weeklyActions[0].outcomes[1].chanceBps"),
            ]),
        ["subscription cash flow"] = (
            WithOutcome(0, 1, static outcome => outcome with { CashFlows = [new CashFlowAmount(CashFlowCategory.Subscriptions, 20)] }),
            [Error(CatalogErrorCodes.InvalidCashFlowCategory, "weeklyActions[0].outcomes[1].cashFlows[0].category")]),
        ["undefined cash flow category"] = (
            WithOutcome(0, 1, static outcome => outcome with { CashFlows = [new CashFlowAmount((CashFlowCategory)99, 20)] }),
            [Error(CatalogErrorCodes.InvalidCashFlowCategory, "weeklyActions[0].outcomes[1].cashFlows[0].category")]),
        ["cash flow of 0 PLN"] = (
            WithOutcome(0, 1, static outcome => outcome with { CashFlows = [DraftCatalog.Donations(0)] }),
            [Error(CatalogErrorCodes.CashFlowAmountOutOfRange, "weeklyActions[0].outcomes[1].cashFlows[0].amountPln")]),
        ["cash flow above the maximum"] = (
            WithOutcome(0, 1, static outcome => outcome with { CashFlows = [DraftCatalog.Donations(MaxAmount + 1)] }),
            [Error(CatalogErrorCodes.CashFlowAmountOutOfRange, "weeklyActions[0].outcomes[1].cashFlows[0].amountPln")]),
        ["viewer gain above the maximum"] = (
            WithOutcome(0, 0, static outcome => outcome with { ViewersDelta = MaxViewers + 1 }),
            [Error(CatalogErrorCodes.ViewersDeltaOutOfRange, "weeklyActions[0].outcomes[0].viewersDelta")]),
        ["viewer loss above the maximum"] = (
            WithOutcome(0, 0, static outcome => outcome with { ViewersDelta = -MaxViewers - 1 }),
            [Error(CatalogErrorCodes.ViewersDeltaOutOfRange, "weeklyActions[0].outcomes[0].viewersDelta")]),
        ["drama +101"] = (
            WithOutcome(0, 0, static outcome => outcome with { DramaDelta = 101 }),
            [Error(CatalogErrorCodes.DramaDeltaOutOfRange, "weeklyActions[0].outcomes[0].dramaDelta")]),
        ["drama -101"] = (
            WithOutcome(0, 0, static outcome => outcome with { DramaDelta = -101 }),
            [Error(CatalogErrorCodes.DramaDeltaOutOfRange, "weeklyActions[0].outcomes[0].dramaDelta")]),
    };

    public static TheoryData<string> ViolationNames => new(Violations.Keys);

    [Fact]
    public void DraftCatalogPasses()
    {
        CatalogValidation validation = GameCatalogValidator.Validate(DraftCatalog.Catalog());

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);
        Assert.Equal(1, validation.Value.Version);
        Assert.Equal(RunStateFixtures.Parameters, validation.Value.Parameters);
        Assert.Equal(
            ["regular_stream", "provocative_stunt", "sponsor_pitch", "quiet_week"],
            validation.Value.WeeklyActions.Select(static action => action.ActionId));
    }

    [Fact]
    public void BoundaryValuesInsideTheRulesPass()
    {
        WeeklyActionDefinition extreme = new(
            "extreme_action",
            MaxAmount,
            [
                DraftCatalog.Outcome("never", 0, viewersDelta: -MaxViewers, dramaDelta: -100, DraftCatalog.Expense(1)),
                DraftCatalog.Outcome("always", 10_000, viewersDelta: MaxViewers, dramaDelta: 100, DraftCatalog.Sponsors(MaxAmount)),
            ]);
        GameCatalog catalog = DraftCatalog.Catalog() with { WeeklyActions = [.. DraftCatalog.WeeklyActions, extreme] };

        CatalogValidation validation = GameCatalogValidator.Validate(catalog);

        Assert.True(validation.IsValid, string.Join(", ", validation.Errors));
    }

    [Theory]
    [MemberData(nameof(ViolationNames))]
    public void ViolationReportsExactlyItsErrors(string name)
    {
        (GameCatalog catalog, CatalogError[] expected) = Violations[name];

        CatalogValidation validation = GameCatalogValidator.Validate(catalog);

        Assert.False(validation.IsValid);
        Assert.Null(validation.Value);
        Assert.Equal(expected, validation.Errors);
    }

    [Fact]
    public void SeveralErrorsAreReportedTogether()
    {
        GameCatalog catalog = DraftCatalog.Catalog() with { Version = 0, WeeklyActions = [] };

        CatalogValidation validation = GameCatalogValidator.Validate(catalog);

        Assert.Equal(
            [Error(CatalogErrorCodes.InvalidCatalogVersion, "version"), Error(CatalogErrorCodes.NoWeeklyActions, "weeklyActions")],
            validation.Errors);
    }

    [Fact]
    public void ValidatedCatalogDoesNotChangeWhenTheInputListsDo()
    {
        List<CashFlowAmount> cashFlows = [DraftCatalog.Sponsors(250)];
        List<WeeklyActionOutcome> outcomes = [new WeeklyActionOutcome("deal", 10_000, ViewersDelta: 5, DramaDelta: 0, cashFlows)];
        List<WeeklyActionDefinition> actions = [new WeeklyActionDefinition("sponsor_pitch", 50, outcomes)];
        CatalogValidation validation = GameCatalogValidator.Validate(new GameCatalog(1, RunStateFixtures.Parameters, actions));
        Assert.True(validation.IsValid);

        cashFlows.Clear();
        outcomes.Clear();
        actions.Clear();

        WeeklyActionDefinition action = Assert.Single(validation.Value.WeeklyActions);
        WeeklyActionOutcome outcome = Assert.Single(action.Outcomes);
        Assert.Equal(DraftCatalog.Sponsors(250), Assert.Single(outcome.CashFlows));
    }

    [Fact]
    public void TryGetActionFindsCatalogueActionsOnly()
    {
        ValidatedCatalog catalog = DraftCatalog.Validated();

        Assert.True(catalog.TryGetAction("sponsor_pitch", out WeeklyActionDefinition? action));
        Assert.Equal(50, action.GuaranteedCostPln);
        Assert.False(catalog.TryGetAction("streaming_marathon", out _));
        Assert.False(catalog.TryGetAction("Sponsor_Pitch", out _));
        Assert.IsNotType<WeeklyActionDefinition[]>(catalog.WeeklyActions);
    }

    [Fact]
    public void AllListsEveryDeclaredCode()
    {
        string[] declared =
        [
            .. typeof(CatalogErrorCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(static field => field.IsLiteral)
                .Select(static field => (string)field.GetRawConstantValue()!),
        ];

        Assert.Equal(declared.Order(StringComparer.Ordinal), CatalogErrorCodes.All.Order(StringComparer.Ordinal));
    }

    private static CatalogError Error(string code, string path)
    {
        return new CatalogError(code, path);
    }

    private static GameCatalog WithAction(int index, Func<WeeklyActionDefinition, WeeklyActionDefinition> change)
    {
        WeeklyActionDefinition[] actions = [.. DraftCatalog.WeeklyActions];
        actions[index] = change(actions[index]);
        return DraftCatalog.Catalog() with { WeeklyActions = actions };
    }

    private static GameCatalog WithOutcomes(
        int actionIndex, Func<IReadOnlyList<WeeklyActionOutcome>, IReadOnlyList<WeeklyActionOutcome>> change)
    {
        return WithAction(actionIndex, action => action with { Outcomes = change(action.Outcomes) });
    }

    private static GameCatalog WithOutcome(int actionIndex, int outcomeIndex, Func<WeeklyActionOutcome, WeeklyActionOutcome> change)
    {
        return WithOutcomes(actionIndex, outcomes =>
        {
            WeeklyActionOutcome[] copy = [.. outcomes];
            copy[outcomeIndex] = change(copy[outcomeIndex]);
            return copy;
        });
    }
}
