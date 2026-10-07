using PolskiStreamerSymulatorApp.Domain.Ledger;

namespace PolskiStreamerSymulatorApp.Domain.Catalog;

/// <summary>
/// One weekly action of a published catalogue: a guaranteed cost paid before the roll and its weighted outcomes.
/// </summary>
public sealed record WeeklyActionDefinition(string ActionId, long GuaranteedCostPln, IReadOnlyList<WeeklyActionOutcome> Outcomes);

/// <summary>
/// One weighted result of a weekly action. Chances are basis points, where 10,000 is 100%.
/// </summary>
public sealed record WeeklyActionOutcome(
    string OutcomeId,
    int ChanceBps,
    int ViewersDelta,
    int DramaDelta,
    IReadOnlyList<CashFlowAmount> CashFlows);

/// <summary>
/// A sponsor or donation income, or an expense, that an outcome adds to the ledger.
/// </summary>
public sealed record CashFlowAmount(CashFlowCategory Category, long AmountPln);
