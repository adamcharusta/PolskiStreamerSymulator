using PolskiStreamerSymulatorApp.Domain.Identity;
using PolskiStreamerSymulatorApp.Domain.Ledger;

namespace PolskiStreamerSymulatorApp.Domain.Catalog;

/// <summary>
/// Checks the rules the weekly engine depends on before any run may use a catalogue.
/// Publication policy, such as translations or at least two distinct outcomes, belongs to the S2 publication check.
/// Every list and every member must be non-null; the catalogue loader guarantees that.
/// </summary>
public static class GameCatalogValidator
{
    /// <summary>Largest guaranteed cost or cash-flow amount, in PLN. An arithmetic guard, not a balance rule.</summary>
    public const long MaxAmountPln = 1_000_000_000;

    /// <summary>Largest viewer change of one outcome, in either direction. An arithmetic guard, not a balance rule.</summary>
    public const int MaxViewersDelta = 1_000_000;

    public const int MaxDramaDelta = GameParametersValidator.MaxDrama;
    public const int TotalChanceBps = 10_000;

    public static CatalogValidation Validate(GameCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(catalog.Parameters);

        if (GameParametersValidator.Validate(catalog.Parameters).Count > 0)
        {
            return CatalogValidation.Failure([new CatalogError(CatalogErrorCodes.InvalidGameParameters, "parameters")]);
        }

        List<CatalogError> errors = [];
        if (catalog.Version < 1)
        {
            errors.Add(new CatalogError(CatalogErrorCodes.InvalidCatalogVersion, "version"));
        }

        if (catalog.WeeklyActions.Count == 0)
        {
            errors.Add(new CatalogError(CatalogErrorCodes.NoWeeklyActions, "weeklyActions"));
        }

        HashSet<string> actionIds = new(StringComparer.Ordinal);
        for (int index = 0; index < catalog.WeeklyActions.Count; index++)
        {
            WeeklyActionDefinition action = catalog.WeeklyActions[index];
            string path = $"weeklyActions[{index}]";
            ValidateId(action.ActionId, $"{path}.actionId", errors);
            if (!actionIds.Add(action.ActionId))
            {
                errors.Add(new CatalogError(CatalogErrorCodes.DuplicateActionId, $"{path}.actionId"));
            }

            if (action.GuaranteedCostPln is < 0 or > MaxAmountPln)
            {
                errors.Add(new CatalogError(CatalogErrorCodes.CostOutOfRange, $"{path}.guaranteedCostPln"));
            }

            ValidateOutcomes(action.Outcomes, path, errors);
        }

        if (errors.Count > 0)
        {
            return CatalogValidation.Failure(errors);
        }

        WeeklyActionDefinition[] copies = [.. catalog.WeeklyActions.Select(CopyAction)];
        return CatalogValidation.Success(new ValidatedCatalog(catalog.Version, catalog.Parameters, copies));
    }

    private static void ValidateOutcomes(IReadOnlyList<WeeklyActionOutcome> outcomes, string actionPath, List<CatalogError> errors)
    {
        if (outcomes.Count == 0)
        {
            errors.Add(new CatalogError(CatalogErrorCodes.NoOutcomes, $"{actionPath}.outcomes"));
            return;
        }

        HashSet<string> outcomeIds = new(StringComparer.Ordinal);
        long totalChance = 0;
        for (int index = 0; index < outcomes.Count; index++)
        {
            WeeklyActionOutcome outcome = outcomes[index];
            string path = $"{actionPath}.outcomes[{index}]";
            ValidateId(outcome.OutcomeId, $"{path}.outcomeId", errors);
            if (!outcomeIds.Add(outcome.OutcomeId))
            {
                errors.Add(new CatalogError(CatalogErrorCodes.DuplicateOutcomeId, $"{path}.outcomeId"));
            }

            if (outcome.ChanceBps is < 0 or > TotalChanceBps)
            {
                errors.Add(new CatalogError(CatalogErrorCodes.ChanceOutOfRange, $"{path}.chanceBps"));
            }

            totalChance += outcome.ChanceBps;
            if (outcome.ViewersDelta is < -MaxViewersDelta or > MaxViewersDelta)
            {
                errors.Add(new CatalogError(CatalogErrorCodes.ViewersDeltaOutOfRange, $"{path}.viewersDelta"));
            }

            if (outcome.DramaDelta is < -MaxDramaDelta or > MaxDramaDelta)
            {
                errors.Add(new CatalogError(CatalogErrorCodes.DramaDeltaOutOfRange, $"{path}.dramaDelta"));
            }

            ValidateCashFlows(outcome.CashFlows, path, errors);
        }

        if (totalChance != TotalChanceBps)
        {
            errors.Add(new CatalogError(CatalogErrorCodes.ChancesDoNotSum, $"{actionPath}.outcomes"));
        }
    }

    private static void ValidateCashFlows(IReadOnlyList<CashFlowAmount> cashFlows, string outcomePath, List<CatalogError> errors)
    {
        for (int index = 0; index < cashFlows.Count; index++)
        {
            CashFlowAmount cashFlow = cashFlows[index];
            string path = $"{outcomePath}.cashFlows[{index}]";
            if (cashFlow.Category is not (CashFlowCategory.Sponsors or CashFlowCategory.Donations or CashFlowCategory.Expenses))
            {
                errors.Add(new CatalogError(CatalogErrorCodes.InvalidCashFlowCategory, $"{path}.category"));
            }

            if (cashFlow.AmountPln is < 1 or > MaxAmountPln)
            {
                errors.Add(new CatalogError(CatalogErrorCodes.CashFlowAmountOutOfRange, $"{path}.amountPln"));
            }
        }
    }

    private static void ValidateId(string id, string path, List<CatalogError> errors)
    {
        if (!StableId.IsValid(id))
        {
            errors.Add(new CatalogError(CatalogErrorCodes.InvalidStableId, path));
        }
    }

    private static WeeklyActionDefinition CopyAction(WeeklyActionDefinition action)
    {
        return action with { Outcomes = [.. action.Outcomes.Select(static outcome => outcome with { CashFlows = [.. outcome.CashFlows] })] };
    }
}
