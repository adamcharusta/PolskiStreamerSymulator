# S1 Weekly Action Engine Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The pure Domain plays a career made only of weekly actions, from a fresh run to its ending, against a validated catalogue. An on-demand sweep reports how action-only careers turn out.

**Architecture:**

- `GameCatalogValidator` turns an untrusted `GameCatalog` into a `ValidatedCatalog` token that holds its own copies of the lists.
- `WeekEngine.PlanWeek` takes a `ValidatedRunState`, a `ValidatedCatalog`, and an action ID, and returns the next `ValidatedRunState` or one stable error code. It runs the atomic action step and, until S3 adds events, completes the week.
- Small pure helpers handle outcome selection (`OutcomeTable`), the subscription formula (`SubscriptionSettlement`), the first state (`RunStarter`), and the weekly report totals (`WeekCashSummary`).
- An explicit xUnit test plays seeded careers and writes a Markdown report.

**Tech Stack:** .NET 10, C# records, xUnit (`xunit.v3` 4.0.1) on Microsoft Testing Platform v2, `Fact(Explicit = true)`.

**Spec:** `docs/superpowers/specs/2026-10-08-s1-weekly-action-engine-design.md`

## Global Constraints

- **Commands:** shell commands run from `PolskiStreamerSymulatorApp/` unless a step starts with its own `cd`. Run tests only as `dotnet test --project <csproj>` or `dotnet test --solution PolskiStreamerSymulatorApp.sln`; `global.json` there selects Microsoft Testing Platform mode.
- **Build:** every build ends with 0 warnings and 0 errors; `Directory.Build.props` treats warnings as errors. `dotnet format PolskiStreamerSymulatorApp.sln --verify-no-changes` must keep passing, because CI checks it.
- **References:** `Domain` keeps no package or project references. Add no package.
- **Namespaces** follow folders:
  - `PolskiStreamerSymulatorApp.Domain.<Folder>`;
  - `PolskiStreamerSymulatorApp.Domain.Tests.<Folder>`.
- **Code style** follows `.editorconfig`: file-scoped namespaces, explicit types instead of `var`, braces, block bodies, LF line endings.
- **Exact values:**
  - `GameCatalogValidator.MaxAmountPln = 1_000_000_000`;
  - `GameCatalogValidator.MaxViewersDelta = 1_000_000`;
  - `GameCatalogValidator.MaxDramaDelta = 100`;
  - `GameCatalogValidator.TotalChanceBps = 10_000`;
  - rolls 0 to 9,999 from `Pcg32.NextRoll`; ledger step 0 for the action step;
  - subscription `floor(max(0, viewers) / SubscriptionViewersPerPln)`;
  - bankruptcy at money less than or equal to `BankruptcyThresholdPln`;
  - catalogue error codes and week-plan error codes exactly as in the code below.
- **Test counts:**
  - `Domain.Tests` starts at 124 and becomes 152 after Task 1, 171 after Task 2, 181 after Task 3, 215 after Task 4, and 216 after Task 5.
  - From Task 5 on, the default run reports 215 succeeded and 1 skipped, because the explicit sweep test is skipped.
  - `Server.IntegrationTests` stays at 71. The final solution run is `total: 287`, `failed: 0`, `succeeded: 286`, `skipped: 1`.
- **Editing files:** create and edit files with a file-writing or editing tool, never with shell heredocs or sed. The shell in this environment turns `\\` into `\`.
- **Staging copy:** every code file below also exists, byte-identical and already compiled and tested, in the staging worktree. An implementer may copy a file from there instead of retyping it, but must still follow the step order and capture the RED output:

  ```
  C:\Users\charu\AppData\Local\Temp\claude\c--Users-charu-Desktop-PolskiStreamerSymulator\2d707178-ee74-461a-9484-0a557d8d8ac4\scratchpad\s1\PolskiStreamerSymulatorApp
  ```

- **Branch:** work stays on `feature/s1-weekly-action-engine`. Never push. End each commit message with the Co-Authored-By attribution trailer your own environment specifies; the commit commands show it as `<your Co-Authored-By trailer>`.
- **Language:** documentation and code comments are in English. This plan changes no player-facing text.

## Review Focus

1. **A save edited so that money or viewers sit near their type limits must fail with `value_out_of_range`, never wrap around.** Owned by Task 4, guard cases "money beyond 64 bits" and "viewers beyond 32 bits".
2. **A retried `PlanWeek` with the same input must return the same week, roll, and generator state, and never mutate or keep the input.** Owned by Task 4: `SameInputGivesTheSameResult`, `PlanningNeitherMutatesNorKeepsTheInputLists`, and `EngineConsumesExactlyOneRollFromTheRunGenerator`.
3. **A debt-funded cost that crosses the threshold must be rescued by its own outcome and the subscription.** At the same time, a step closing at exactly -1,000 PLN must bankrupt the run, even in the final week. Owned by Task 4: `OutcomeAndSubscriptionRescueADebtFundedCost`, `StepClosingAtTheThresholdBankruptsTheRun`, and `BankruptcyTakesPriorityInTheFinalWeek`.
4. **A catalogue list changed after validation, for example in a cache, must not change the validated catalogue.** Owned by Task 1: `ValidatedCatalogDoesNotChangeWhenTheInputListsDo`.
5. **A weekly report for an edited save with huge ledger amounts must throw, not show wrapped totals.** Owned by Task 3: `TotalsBeyondSixtyFourBitsThrowInsteadOfWrapping`.

## File Structure

| Path under `PolskiStreamerSymulatorApp/` | Responsibility |
| --- | --- |
| `src/Domain/Catalog/GameCatalog.cs` | Untrusted catalogue shape |
| `src/Domain/Catalog/CatalogValidation.cs` | `CatalogValidation`, `ValidatedCatalog`, `CatalogError` |
| `src/Domain/Catalog/CatalogErrorCodes.cs` | Stable catalogue error codes |
| `src/Domain/Catalog/GameCatalogValidator.cs` | Catalogue rules and engine limits |
| `src/Domain/Engine/OutcomeTable.cs` | Running-sum outcome selection |
| `src/Domain/Engine/SubscriptionSettlement.cs` | Weekly subscription formula |
| `src/Domain/Engine/WeekPlanResult.cs`, `WeekPlanErrorCodes.cs` | Engine result and error codes |
| `src/Domain/Engine/WeekEngine.cs` | Action step and week completion |
| `src/Domain/Runs/RunStarter.cs` | Week-1 state |
| `src/Domain/Runs/WeekCashSummary.cs` | Weekly report totals |
| `tests/Domain.Tests/Catalog/DraftCatalog.cs` | The four draft actions as test data |
| `tests/Domain.Tests/Runs/RunStateAssert.cs` | Structural state comparison |
| `tests/Domain.Tests/Balance/ActionOnlySweep.cs`, `ActionOnlySweepTests.cs` | Explicit balance sweep |
| `tests/Domain.Tests/**/*Tests.cs` | Unit tests |

---

### Task 1: Game catalogue and its validator

**Files:**
- Create: `PolskiStreamerSymulatorApp/src/Domain/Catalog/GameCatalog.cs`, `CatalogValidation.cs`, `CatalogErrorCodes.cs`, `GameCatalogValidator.cs`
- Create: `PolskiStreamerSymulatorApp/tests/Domain.Tests/Catalog/DraftCatalog.cs`, `GameCatalogValidatorTests.cs`

**Interfaces:**
- Consumes: `GameParameters`, `GameParametersValidator` (with `MaxDrama`), `WeeklyActionDefinition`, `WeeklyActionOutcome`, `CashFlowAmount`, `CashFlowCategory`, `StableId.IsValid`, and the test fixture `RunStateFixtures.Parameters`, all from F2.
- Produces, in `PolskiStreamerSymulatorApp.Domain.Catalog`:
  - `GameCatalog(int Version, GameParameters Parameters, IReadOnlyList<WeeklyActionDefinition> WeeklyActions)`;
  - `GameCatalogValidator.Validate(GameCatalog) : CatalogValidation`, with constants `MaxAmountPln`, `MaxViewersDelta`, `MaxDramaDelta`, and `TotalChanceBps`;
  - `CatalogValidation` (`IsValid`, `Errors`, `Value`);
  - `ValidatedCatalog` (`Version`, `Parameters`, `WeeklyActions`, `TryGetAction(string, out WeeklyActionDefinition?)`, internal constructor);
  - `CatalogError(string Code, string Path)`;
  - `CatalogErrorCodes` with `All`.
- Produces, in tests: `DraftCatalog` with
  - `WeeklyActions`;
  - `Catalog(GameParameters? = null)`;
  - `Validated(GameParameters? = null)`;
  - `SingleAction(GameParameters, long costPln, int viewersDelta, int dramaDelta, params CashFlowAmount[])`, whose action ID is `test_action`;
  - `Outcome`, `Sponsors`, `Donations`, and `Expense`.

- [ ] **Step 1: Write the test data and the failing tests**

Create `tests/Domain.Tests/Catalog/DraftCatalog.cs`:

```csharp
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
```

Create `tests/Domain.Tests/Catalog/GameCatalogValidatorTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run the tests and see them fail**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj`

Expected: the build fails with errors CS0103 and CS0246, because `GameCatalog`, `CatalogValidation`, `ValidatedCatalog`, `CatalogError`, `CatalogErrorCodes`, and `GameCatalogValidator` do not exist yet.

- [ ] **Step 3: Implement the catalogue and its validator**

Create `src/Domain/Catalog/GameCatalog.cs`:

```csharp
namespace PolskiStreamerSymulatorApp.Domain.Catalog;

/// <summary>
/// The published content a run plays against: its version, its typed parameters, and its enabled weekly actions in catalogue order.
/// Untrusted until GameCatalogValidator turns it into a ValidatedCatalog.
/// </summary>
public sealed record GameCatalog(int Version, GameParameters Parameters, IReadOnlyList<WeeklyActionDefinition> WeeklyActions);
```

Create `src/Domain/Catalog/CatalogValidation.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;

namespace PolskiStreamerSymulatorApp.Domain.Catalog;

/// <summary>
/// Outcome of validating a catalogue: either errors or a catalogue the engine may use.
/// </summary>
public sealed class CatalogValidation
{
    private CatalogValidation(IReadOnlyList<CatalogError> errors, ValidatedCatalog? value)
    {
        Errors = errors;
        Value = value;
    }

    public IReadOnlyList<CatalogError> Errors { get; }

    public ValidatedCatalog? Value { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsValid => Value is not null;

    internal static CatalogValidation Success(ValidatedCatalog value)
    {
        return new CatalogValidation([], value);
    }

    internal static CatalogValidation Failure(IReadOnlyList<CatalogError> errors)
    {
        return new CatalogValidation(errors, null);
    }
}

/// <summary>
/// A catalogue that passed validation. Only GameCatalogValidator creates it, from copies of the caller's lists,
/// so later changes to those lists cannot change it.
/// </summary>
public sealed class ValidatedCatalog
{
    private readonly Dictionary<string, WeeklyActionDefinition> _actionsById;

    internal ValidatedCatalog(int version, GameParameters parameters, IReadOnlyList<WeeklyActionDefinition> weeklyActions)
    {
        Version = version;
        Parameters = parameters;
        WeeklyActions = weeklyActions;
        _actionsById = weeklyActions.ToDictionary(static action => action.ActionId, StringComparer.Ordinal);
    }

    public int Version { get; }

    public GameParameters Parameters { get; }

    /// <summary>
    /// The weekly actions in catalogue order.
    /// </summary>
    public IReadOnlyList<WeeklyActionDefinition> WeeklyActions { get; }

    public bool TryGetAction(string actionId, [NotNullWhen(true)] out WeeklyActionDefinition? action)
    {
        ArgumentNullException.ThrowIfNull(actionId);
        return _actionsById.TryGetValue(actionId, out action);
    }
}

/// <summary>
/// One catalogue validation failure: a stable code from CatalogErrorCodes and the path of the offending value.
/// </summary>
public sealed record CatalogError(string Code, string Path);
```

Create `src/Domain/Catalog/CatalogErrorCodes.cs`:

```csharp
namespace PolskiStreamerSymulatorApp.Domain.Catalog;

/// <summary>
/// Stable codes for catalogue validation failures. The S1 design spec states the rule behind each code.
/// </summary>
public static class CatalogErrorCodes
{
    public const string InvalidCatalogVersion = "invalid_catalog_version";
    public const string InvalidGameParameters = "invalid_game_parameters";
    public const string NoWeeklyActions = "no_weekly_actions";
    public const string InvalidStableId = "invalid_stable_id";
    public const string DuplicateActionId = "duplicate_action_id";
    public const string DuplicateOutcomeId = "duplicate_outcome_id";
    public const string CostOutOfRange = "cost_out_of_range";
    public const string NoOutcomes = "no_outcomes";
    public const string ChanceOutOfRange = "chance_out_of_range";
    public const string ChancesDoNotSum = "chances_do_not_sum";
    public const string InvalidCashFlowCategory = "invalid_cash_flow_category";
    public const string CashFlowAmountOutOfRange = "cash_flow_amount_out_of_range";
    public const string ViewersDeltaOutOfRange = "viewers_delta_out_of_range";
    public const string DramaDeltaOutOfRange = "drama_delta_out_of_range";

    public static IReadOnlyList<string> All { get; } =
    [
        InvalidCatalogVersion,
        InvalidGameParameters,
        NoWeeklyActions,
        InvalidStableId,
        DuplicateActionId,
        DuplicateOutcomeId,
        CostOutOfRange,
        NoOutcomes,
        ChanceOutOfRange,
        ChancesDoNotSum,
        InvalidCashFlowCategory,
        CashFlowAmountOutOfRange,
        ViewersDeltaOutOfRange,
        DramaDeltaOutOfRange,
    ];
}
```

Create `src/Domain/Catalog/GameCatalogValidator.cs`:

```csharp
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
```

- [ ] **Step 4: Run the tests and see them pass**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj`

Expected: `Passed!` with `total: 152` and `failed: 0`.

- [ ] **Step 5: Commit**

```bash
cd .. && git add PolskiStreamerSymulatorApp/src/Domain/Catalog PolskiStreamerSymulatorApp/tests/Domain.Tests/Catalog && git commit -m "Add the game catalogue and its validator" -m "<your Co-Authored-By trailer>"
```

---

### Task 2: Outcome selection and the subscription formula

**Files:**
- Create: `PolskiStreamerSymulatorApp/src/Domain/Engine/OutcomeTable.cs`, `SubscriptionSettlement.cs`
- Create: `PolskiStreamerSymulatorApp/tests/Domain.Tests/Engine/OutcomeTableTests.cs`, `SubscriptionSettlementTests.cs`

**Interfaces:**
- Consumes: `GameCatalogValidator.TotalChanceBps` and `DraftCatalog` from Task 1; `Pcg32.RollBound` from F2.
- Produces, in `PolskiStreamerSymulatorApp.Domain.Engine`:
  - `OutcomeTable.Select(IReadOnlyList<WeeklyActionOutcome> outcomes, int roll) : WeeklyActionOutcome`. It throws `ArgumentOutOfRangeException` for a roll outside 0 to 9,999, and `ArgumentException` for a table that is not non-negative and summing to 10,000.
  - `SubscriptionSettlement.WeeklyAmountPln(int viewers, int subscriptionViewersPerPln) : long`. It throws `ArgumentOutOfRangeException` for a divisor below 1.

- [ ] **Step 1: Write the failing tests**

Create `tests/Domain.Tests/Engine/OutcomeTableTests.cs`:

```csharp
using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Engine;
using PolskiStreamerSymulatorApp.Domain.Tests.Catalog;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Engine;

public sealed class OutcomeTableTests
{
    private static IReadOnlyList<WeeklyActionOutcome> RegularStream => DraftCatalog.WeeklyActions[0].Outcomes;

    [Theory]
    [InlineData(0, "steady")]
    [InlineData(5_999, "steady")]
    [InlineData(6_000, "good_chat")]
    [InlineData(8_999, "good_chat")]
    [InlineData(9_000, "slow_evening")]
    [InlineData(9_999, "slow_evening")]
    public void RollSelectsTheOutcomeWhoseIntervalContainsIt(int roll, string expectedOutcomeId)
    {
        WeeklyActionOutcome outcome = OutcomeTable.Select(RegularStream, roll);

        Assert.Equal(expectedOutcomeId, outcome.OutcomeId);
    }

    [Fact]
    public void ZeroChanceOutcomeIsNeverSelected()
    {
        WeeklyActionOutcome[] outcomes =
        [
            DraftCatalog.Outcome("never_first", 0, viewersDelta: 0, dramaDelta: 0),
            DraftCatalog.Outcome("always", 10_000, viewersDelta: 0, dramaDelta: 0),
            DraftCatalog.Outcome("never_last", 0, viewersDelta: 0, dramaDelta: 0),
        ];

        string[] selected = [.. Enumerable.Range(0, 10_000).Select(roll => OutcomeTable.Select(outcomes, roll).OutcomeId).Distinct()];

        Assert.Equal(["always"], selected);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10_000)]
    public void RollOutsideZeroToNineThousandNineHundredNinetyNineIsRejected(int roll)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OutcomeTable.Select(RegularStream, roll));
    }

    [Fact]
    public void TableThatDoesNotSumToTenThousandIsRejected()
    {
        WeeklyActionOutcome[] outcomes = [DraftCatalog.Outcome("partial", 9_999, viewersDelta: 0, dramaDelta: 0)];

        Assert.Throws<ArgumentException>(() => OutcomeTable.Select(outcomes, 0));
    }
}
```

Create `tests/Domain.Tests/Engine/SubscriptionSettlementTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run the tests and see them fail**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj`

Expected: the build fails with error CS0234 or CS0246, because the `PolskiStreamerSymulatorApp.Domain.Engine` namespace, `OutcomeTable`, and `SubscriptionSettlement` do not exist yet.

- [ ] **Step 3: Implement outcome selection and the formula**

Create `src/Domain/Engine/OutcomeTable.cs`:

```csharp
using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Randomness;

namespace PolskiStreamerSymulatorApp.Domain.Engine;

/// <summary>
/// Selects the weighted outcome a roll lands on. Outcomes are walked in catalogue order with a running sum of their chances,
/// and the first outcome whose running sum exceeds the roll wins, so an outcome with a chance of 0 is never selected.
/// </summary>
public static class OutcomeTable
{
    public static WeeklyActionOutcome Select(IReadOnlyList<WeeklyActionOutcome> outcomes, int roll)
    {
        ArgumentNullException.ThrowIfNull(outcomes);
        ArgumentOutOfRangeException.ThrowIfNegative(roll);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(roll, (int)Pcg32.RollBound);
        if (outcomes.Any(static outcome => outcome.ChanceBps < 0)
            || outcomes.Sum(static outcome => (long)outcome.ChanceBps) != GameCatalogValidator.TotalChanceBps)
        {
            throw new ArgumentException("Outcome chances must be non-negative and sum to 10,000 basis points.", nameof(outcomes));
        }

        int runningSum = 0;
        foreach (WeeklyActionOutcome outcome in outcomes)
        {
            runningSum += outcome.ChanceBps;
            if (roll < runningSum)
            {
                return outcome;
            }
        }

        throw new InvalidOperationException("Unreachable: the chances sum to 10,000 and the roll is below 10,000.");
    }
}
```

Create `src/Domain/Engine/SubscriptionSettlement.cs`:

```csharp
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
```

- [ ] **Step 4: Run the tests and see them pass**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj`

Expected: `Passed!` with `total: 171` and `failed: 0`.

- [ ] **Step 5: Commit**

```bash
cd .. && git add PolskiStreamerSymulatorApp/src/Domain/Engine PolskiStreamerSymulatorApp/tests/Domain.Tests/Engine && git commit -m "Add outcome selection and the subscription formula" -m "<your Co-Authored-By trailer>"
```

---

### Task 3: Run start and the weekly cash summary

**Files:**
- Create: `PolskiStreamerSymulatorApp/src/Domain/Runs/RunStarter.cs`, `WeekCashSummary.cs`
- Create: `PolskiStreamerSymulatorApp/tests/Domain.Tests/Runs/RunStarterTests.cs`, `WeekCashSummaryTests.cs`

**Interfaces:**
- Consumes:
  - from Task 1: `ValidatedCatalog` and `DraftCatalog`;
  - from F2: `StreamerName.TryNormalize`, `Pcg32.Seed`, `RulesVersion.Current`, `RunStateValidator.Validate`, `RunStateValidation`, `ValidatedRunState`;
  - from F2's tests: `RunStateFixtures` (`RunId`, `Parameters`, `Active()`, `PendingWeek()`, `QuietFirstWeek()`, `Subscription(int, long)`).
- Produces, in `PolskiStreamerSymulatorApp.Domain.Runs`:
  - `RunStarter.Start(ValidatedCatalog catalog, Guid runId, string streamerName, ulong seed, ulong stream) : RunStateValidation`;
  - `WeekCashSummary(int Week, long OpeningMoneyPln, long SponsorsPln, long DonationsPln, long SubscriptionsPln, long ExpensesPln, long ClosingMoneyPln)`, with `static WeekCashSummary For(ValidatedRunState run, int week)`. `For` throws `ArgumentOutOfRangeException` for a week without a record, and `OverflowException` for totals beyond 64 bits.

- [ ] **Step 1: Write the failing tests**

Create `tests/Domain.Tests/Runs/RunStarterTests.cs`:

```csharp
using System.Text;
using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Randomness;
using PolskiStreamerSymulatorApp.Domain.Runs;
using PolskiStreamerSymulatorApp.Domain.Tests.Catalog;
using PolskiStreamerSymulatorApp.Domain.Versioning;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Runs;

public sealed class RunStarterTests
{
    [Fact]
    public void StartsWeekOneFromThePinnedParameters()
    {
        RunStateValidation start = RunStarter.Start(DraftCatalog.Validated(), RunStateFixtures.RunId, "NeonBorsuk", seed: 42, stream: 54);

        Assert.True(start.IsValid, string.Join(", ", start.Errors));
        RunState state = start.Value.State;
        Assert.Equal(RulesVersion.Current, state.RulesVersion);
        Assert.Equal(1, state.CatalogVersion);
        Assert.Equal(RunStateFixtures.RunId, state.RunId);
        Assert.Equal("NeonBorsuk", state.StreamerName);
        Assert.Equal(1, state.Week);
        Assert.Equal(RunStatus.Active, state.Status);
        Assert.Equal(1_500, state.MoneyPln);
        Assert.Equal(20, state.Viewers);
        Assert.Equal(50, state.Drama);
        Assert.Equal(Pcg32.Seed(42, 54), state.Rng);
        Assert.Empty(state.Ledger);
        Assert.Empty(state.History);
        Assert.Null(state.CurrentWeek);
        Assert.Empty(state.Flags);
        Assert.Null(state.Ending);
        Assert.Equal(RunStateFixtures.Parameters, start.Value.Parameters);
    }

    [Fact]
    public void UsesTheCatalogueParametersRatherThanFirstPublishedValues()
    {
        GameParameters parameters = RunStateFixtures.Parameters with { StartingMoneyPln = 900, StartingViewers = 0, StartingDrama = 70 };

        RunStateValidation start = RunStarter.Start(DraftCatalog.Validated(parameters), RunStateFixtures.RunId, "NeonBorsuk", 1, 1);

        Assert.True(start.IsValid);
        Assert.Equal((900L, 0, 70), (start.Value.State.MoneyPln, start.Value.State.Viewers, start.Value.State.Drama));
    }

    [Fact]
    public void StoresTheNormalizedName()
    {
        string decomposed = "Zażółć".Normalize(NormalizationForm.FormD);

        RunStateValidation start = RunStarter.Start(DraftCatalog.Validated(), RunStateFixtures.RunId, $"  {decomposed} ", 1, 1);

        Assert.True(start.IsValid);
        Assert.Equal("Zażółć".Normalize(NormalizationForm.FormC), start.Value.State.StreamerName);
    }

    [Fact]
    public void InvalidNameIsReportedWithTheRunStateCode()
    {
        RunStateValidation start = RunStarter.Start(DraftCatalog.Validated(), RunStateFixtures.RunId, "A", 1, 1);

        Assert.False(start.IsValid);
        Assert.Equal([new RunStateError(RunStateErrorCodes.InvalidStreamerName, "streamerName")], start.Errors);
    }

    [Fact]
    public void EmptyRunIdIsReportedWithTheRunStateCode()
    {
        RunStateValidation start = RunStarter.Start(DraftCatalog.Validated(), Guid.Empty, "NeonBorsuk", 1, 1);

        Assert.False(start.IsValid);
        Assert.Equal([new RunStateError(RunStateErrorCodes.InvalidRunId, "runId")], start.Errors);
    }
}
```

Create `tests/Domain.Tests/Runs/WeekCashSummaryTests.cs`:

```csharp
using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Runs;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Runs;

public sealed class WeekCashSummaryTests
{
    [Fact]
    public void BalanceIllustrationReconcilesToOneThousandSixHundredNinetyThree()
    {
        // The ledger example from balance.md: 1,500 + 200 + 35 + 18 - 60 = 1,693 PLN.
        RunState state = RunStateFixtures.Active() with
        {
            MoneyPln = 1_693,
            Viewers = 25,
            Ledger =
            [
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionCost, 0, CashFlowCategory.Expenses, 60),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 0, CashFlowCategory.Sponsors, 200),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 1, CashFlowCategory.Donations, 35),
                RunStateFixtures.Subscription(1, 18),
            ],
            History =
            [
                RunStateFixtures.QuietFirstWeek() with
                {
                    Action = new ActionResolution("sponsor_pitch", Roll: 100, OutcomeId: "deal", ViewersDelta: 5, DramaDelta: 0),
                },
            ],
        };

        WeekCashSummary summary = WeekCashSummary.For(Validate(state), week: 1);

        Assert.Equal(new WeekCashSummary(1, 1_500, 200, 35, 18, 60, 1_693), summary);
    }

    [Fact]
    public void PendingWeekCoversTheWeekSoFar()
    {
        WeekCashSummary summary = WeekCashSummary.For(Validate(RunStateFixtures.PendingWeek()), week: 2);

        Assert.Equal(new WeekCashSummary(2, 1_502, 250, 0, 3, 80, 1_675), summary);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void WeekWithoutARecordIsRejected(int week)
    {
        ValidatedRunState run = Validate(RunStateFixtures.Active());

        Assert.Throws<ArgumentOutOfRangeException>(() => WeekCashSummary.For(run, week));
    }

    [Fact]
    public void TotalsBeyondSixtyFourBitsThrowInsteadOfWrapping()
    {
        // An edited save may hold huge entries that still reconcile; their week total does not fit in 64 bits.
        RunState state = RunStateFixtures.Active() with
        {
            Ledger =
            [
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 0, CashFlowCategory.Sponsors, long.MaxValue),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 1, CashFlowCategory.Sponsors, long.MaxValue),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 2, CashFlowCategory.Expenses, long.MaxValue),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 3, CashFlowCategory.Expenses, long.MaxValue),
                RunStateFixtures.Subscription(1, 2),
            ],
        };
        ValidatedRunState run = Validate(state);

        Assert.Throws<OverflowException>(() => WeekCashSummary.For(run, week: 1));
    }

    private static ValidatedRunState Validate(RunState state)
    {
        RunStateValidation validation = RunStateValidator.Validate(state, RunStateFixtures.Parameters);
        Assert.True(validation.IsValid, string.Join(", ", validation.Errors));
        return validation.Value;
    }
}
```

- [ ] **Step 2: Run the tests and see them fail**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj`

Expected: the build fails with error CS0103 or CS0246, because `RunStarter` and `WeekCashSummary` do not exist yet.

- [ ] **Step 3: Implement run start and the summary**

Create `src/Domain/Runs/RunStarter.cs`:

```csharp
using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Identity;
using PolskiStreamerSymulatorApp.Domain.Randomness;
using PolskiStreamerSymulatorApp.Domain.Versioning;

namespace PolskiStreamerSymulatorApp.Domain.Runs;

/// <summary>
/// Builds the first state of a new career from a validated catalogue. The caller supplies the run ID and the generator seed and stream.
/// </summary>
public static class RunStarter
{
    /// <summary>
    /// Returns the validated week-1 state, or the run-state validation errors for an invalid name or an empty run ID.
    /// </summary>
    public static RunStateValidation Start(ValidatedCatalog catalog, Guid runId, string streamerName, ulong seed, ulong stream)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(streamerName);

        GameParameters parameters = catalog.Parameters;
        string name = StreamerName.TryNormalize(streamerName, out string normalized) ? normalized : streamerName;
        RunState state = new(
            RulesVersion: RulesVersion.Current,
            CatalogVersion: catalog.Version,
            RunId: runId,
            StreamerName: name,
            Week: 1,
            Status: RunStatus.Active,
            MoneyPln: parameters.StartingMoneyPln,
            Viewers: parameters.StartingViewers,
            Drama: parameters.StartingDrama,
            Rng: Pcg32.Seed(seed, stream),
            Ledger: [],
            History: [],
            CurrentWeek: null,
            Flags: [],
            Ending: null);
        return RunStateValidator.Validate(state, parameters);
    }
}
```

Create `src/Domain/Runs/WeekCashSummary.cs`:

```csharp
using PolskiStreamerSymulatorApp.Domain.Ledger;

namespace PolskiStreamerSymulatorApp.Domain.Runs;

/// <summary>
/// The money view of one week for its report: opening money, the four category totals as non-negative amounts, and closing money,
/// where closing = opening + sponsors + donations + subscriptions - expenses. For a pending week it covers the week so far.
/// </summary>
public sealed record WeekCashSummary(
    int Week,
    long OpeningMoneyPln,
    long SponsorsPln,
    long DonationsPln,
    long SubscriptionsPln,
    long ExpensesPln,
    long ClosingMoneyPln)
{
    /// <summary>
    /// Sums in 128-bit arithmetic and throws OverflowException, rather than wrapping, when an edited save's totals do not fit in 64 bits.
    /// </summary>
    public static WeekCashSummary For(ValidatedRunState run, int week)
    {
        ArgumentNullException.ThrowIfNull(run);
        RunState state = run.State;
        bool hasRecord = state.History.Any(record => record.Week == week) || state.CurrentWeek?.Week == week;
        if (!hasRecord)
        {
            throw new ArgumentOutOfRangeException(nameof(week), week, "The run has no record for this week.");
        }

        Int128 opening = run.Parameters.StartingMoneyPln;
        Int128 sponsors = 0;
        Int128 donations = 0;
        Int128 subscriptions = 0;
        Int128 expenses = 0;
        foreach (CashFlowEntry entry in state.Ledger)
        {
            if (entry.Week < week)
            {
                opening += entry.SignedAmountPln;
            }
            else if (entry.Week == week)
            {
                switch (entry.Category)
                {
                    case CashFlowCategory.Sponsors:
                        sponsors += entry.AmountPln;
                        break;
                    case CashFlowCategory.Donations:
                        donations += entry.AmountPln;
                        break;
                    case CashFlowCategory.Subscriptions:
                        subscriptions += entry.AmountPln;
                        break;
                    case CashFlowCategory.Expenses:
                        expenses += entry.AmountPln;
                        break;
                }
            }
        }

        Int128 closing = opening + sponsors + donations + subscriptions - expenses;
        return new WeekCashSummary(
            week,
            checked((long)opening),
            checked((long)sponsors),
            checked((long)donations),
            checked((long)subscriptions),
            checked((long)expenses),
            checked((long)closing));
    }
}
```

- [ ] **Step 4: Run the tests and see them pass**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj`

Expected: `Passed!` with `total: 181` and `failed: 0`.

- [ ] **Step 5: Commit**

```bash
cd .. && git add PolskiStreamerSymulatorApp/src/Domain/Runs/RunStarter.cs PolskiStreamerSymulatorApp/src/Domain/Runs/WeekCashSummary.cs PolskiStreamerSymulatorApp/tests/Domain.Tests/Runs/RunStarterTests.cs PolskiStreamerSymulatorApp/tests/Domain.Tests/Runs/WeekCashSummaryTests.cs && git commit -m "Add run start and the weekly cash summary" -m "<your Co-Authored-By trailer>"
```

---

### Task 4: Weekly action engine

**Files:**
- Create: `PolskiStreamerSymulatorApp/src/Domain/Engine/WeekPlanResult.cs`, `WeekPlanErrorCodes.cs`, `WeekEngine.cs`
- Create: `PolskiStreamerSymulatorApp/tests/Domain.Tests/Runs/RunStateAssert.cs`
- Create: `PolskiStreamerSymulatorApp/tests/Domain.Tests/Engine/WeekEngineTests.cs`, `FullCareerTests.cs`

**Interfaces:**
- Consumes: everything from Tasks 1 to 3, and from F2 the `RunState` records, `CashFlowEntry`, `Pcg32`, the `RunStateFixtures` states, `TwoWeekParameters`, and `InDebtParameters`.
- Produces, in `PolskiStreamerSymulatorApp.Domain.Engine`:
  - `WeekEngine.PlanWeek(ValidatedRunState run, ValidatedCatalog catalog, string actionId) : WeekPlanResult`;
  - `WeekPlanResult` (`IsSuccess`, `Value`, `ErrorCode`);
  - `WeekPlanErrorCodes`: `catalog_mismatch`, `week_pending`, `run_finished`, `unknown_action`, `value_out_of_range`, with `All`.
- Produces, in tests: `RunStateAssert.Equivalent(RunState expected, RunState actual)`.

- [ ] **Step 1: Add the structural comparison helper**

Create `tests/Domain.Tests/Runs/RunStateAssert.cs`:

```csharp
using PolskiStreamerSymulatorApp.Domain.Runs;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Runs;

/// <summary>
/// Structural equality for run states. Records compare their list members by reference, so lists are compared element by element.
/// </summary>
internal static class RunStateAssert
{
    public static void Equivalent(RunState expected, RunState actual)
    {
        Assert.Equal(Scalars(expected), Scalars(actual));
        Assert.Equal(expected.Ledger, actual.Ledger);
        Assert.Equal(expected.Flags, actual.Flags);
        Assert.Equal(expected.History.Count, actual.History.Count);
        for (int index = 0; index < expected.History.Count; index++)
        {
            Week(expected.History[index], actual.History[index]);
        }

        if (expected.CurrentWeek is null || actual.CurrentWeek is null)
        {
            Assert.Equal(expected.CurrentWeek is null, actual.CurrentWeek is null);
            return;
        }

        Assert.Equal(
            (expected.CurrentWeek.Week, expected.CurrentWeek.Action, expected.CurrentWeek.Pending),
            (actual.CurrentWeek.Week, actual.CurrentWeek.Action, actual.CurrentWeek.Pending));
        Assert.Equal(expected.CurrentWeek.EncounterRolls, actual.CurrentWeek.EncounterRolls);
        Events(expected.CurrentWeek.ResolvedEvents, actual.CurrentWeek.ResolvedEvents);
    }

    private static object Scalars(RunState state)
    {
        return (
            state.RulesVersion,
            state.CatalogVersion,
            state.RunId,
            state.StreamerName,
            state.Week,
            state.Status,
            state.MoneyPln,
            state.Viewers,
            state.Drama,
            state.Rng,
            state.Ending);
    }

    private static object ResponseScalars(ResponseResolution response)
    {
        return (response.OptionId, response.Roll, response.OutcomeId, response.ViewersDelta, response.DramaDelta, response.TerminalReasonCode);
    }

    private static void Week(WeekRecord expected, WeekRecord actual)
    {
        Assert.Equal((expected.Week, expected.Action), (actual.Week, actual.Action));
        Assert.Equal(expected.EncounterRolls, actual.EncounterRolls);
        Events(expected.Events, actual.Events);
    }

    private static void Events(IReadOnlyList<EventResolution> expected, IReadOnlyList<EventResolution> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int index = 0; index < expected.Count; index++)
        {
            Assert.Equal((expected[index].EventId, expected[index].Index), (actual[index].EventId, actual[index].Index));
            ResponseResolution? expectedResponse = expected[index].Response;
            ResponseResolution? actualResponse = actual[index].Response;
            if (expectedResponse is null || actualResponse is null)
            {
                Assert.Equal(expectedResponse is null, actualResponse is null);
                continue;
            }

            Assert.Equal(ResponseScalars(expectedResponse), ResponseScalars(actualResponse));
            Assert.Equal(expectedResponse.FlagChanges, actualResponse.FlagChanges);
        }
    }
}
```

- [ ] **Step 2: Write the failing engine tests**

Create `tests/Domain.Tests/Engine/WeekEngineTests.cs`:

```csharp
using System.Reflection;
using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Engine;
using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Randomness;
using PolskiStreamerSymulatorApp.Domain.Runs;
using PolskiStreamerSymulatorApp.Domain.Tests.Catalog;
using PolskiStreamerSymulatorApp.Domain.Tests.Runs;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Engine;

public sealed class WeekEngineTests
{
    private const string TestAction = "test_action";

    private static readonly Dictionary<string, Func<(ValidatedRunState Run, ValidatedCatalog Catalog, string ActionId, string Code)>> Guards = new()
    {
        ["catalogue version differs"] = static () =>
            (Start(DraftCatalog.Validated()), Revalidate(DraftCatalog.Catalog() with { Version = 2 }), "regular_stream",
                WeekPlanErrorCodes.CatalogMismatch),
        ["catalogue parameters differ"] = static () =>
            (Start(DraftCatalog.Validated()),
                DraftCatalog.Validated(RunStateFixtures.Parameters with { SubscriptionViewersPerPln = 5 }),
                "regular_stream",
                WeekPlanErrorCodes.CatalogMismatch),
        ["pending week"] = static () =>
            (Validate(RunStateFixtures.PendingWeek(), RunStateFixtures.Parameters), DraftCatalog.Validated(), "regular_stream",
                WeekPlanErrorCodes.WeekPending),
        ["completed run"] = static () =>
            (Validate(RunStateFixtures.Completed(), RunStateFixtures.TwoWeekParameters),
                DraftCatalog.Validated(RunStateFixtures.TwoWeekParameters),
                "regular_stream",
                WeekPlanErrorCodes.RunFinished),
        ["bankrupt run"] = static () =>
            (Validate(RunStateFixtures.Bankrupt(), RunStateFixtures.InDebtParameters),
                DraftCatalog.Validated(RunStateFixtures.InDebtParameters),
                "regular_stream",
                WeekPlanErrorCodes.RunFinished),
        ["special ending"] = static () =>
            (Validate(RunStateFixtures.SpecialEnding(), RunStateFixtures.Parameters), DraftCatalog.Validated(), "regular_stream",
                WeekPlanErrorCodes.RunFinished),
        ["unknown action"] = static () =>
            (Start(DraftCatalog.Validated()), DraftCatalog.Validated(), "streaming_marathon", WeekPlanErrorCodes.UnknownAction),
        ["money beyond 64 bits"] = static () => OutOfRange(
            RunStateFixtures.Parameters with { StartingMoneyPln = long.MaxValue }, viewersDelta: 0, DraftCatalog.Donations(80)),
        ["viewers beyond 32 bits"] = static () => OutOfRange(
            RunStateFixtures.Parameters with { StartingViewers = int.MaxValue }, viewersDelta: 45),
    };

    public static TheoryData<string> GuardNames => new(Guards.Keys);

    [Fact]
    public void FirstWeekMatchesTheKnownAnswer()
    {
        // Seed 42 on stream 54 rolls 1,783 first, which lands on the steady outcome of a regular stream.
        Pcg32 generator = new(Pcg32.Seed(42, 54));
        generator.NextRoll();
        RunState expected = RunStateFixtures.Active() with
        {
            Rng = generator.ToState(),
            History = [RunStateFixtures.QuietFirstWeek() with { EncounterRolls = [] }],
        };

        RunState actual = Plan(Start(DraftCatalog.Validated()), DraftCatalog.Validated(), "regular_stream");

        RunStateAssert.Equivalent(expected, actual);
    }

    [Fact]
    public void FreeActionRecordsOnlyTheSubscription()
    {
        RunState state = Plan(Start(DraftCatalog.Validated()), DraftCatalog.Validated(), "regular_stream");

        Assert.Equal([RunStateFixtures.Subscription(1, 2)], state.Ledger);
    }

    [Fact]
    public void PaidActionRecordsItsCostOnceBeforeTheOutcome()
    {
        // Roll 1,783 lands on the deal outcome of a sponsor pitch.
        RunState state = Plan(Start(DraftCatalog.Validated()), DraftCatalog.Validated(), "sponsor_pitch");

        Assert.Equal(
            [
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionCost, 0, CashFlowCategory.Expenses, 50),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 0, CashFlowCategory.Sponsors, 250),
                RunStateFixtures.Subscription(1, 2),
            ],
            state.Ledger);
        Assert.Equal(1_702, state.MoneyPln);
    }

    [Fact]
    public void OutcomeCashFlowsKeepTheirOrdinals()
    {
        ValidatedCatalog catalog = DraftCatalog.SingleAction(
            RunStateFixtures.Parameters, costPln: 0, viewersDelta: 0, dramaDelta: 0, DraftCatalog.Sponsors(100), DraftCatalog.Expense(30));

        RunState state = Plan(Start(catalog), catalog, TestAction);

        Assert.Equal(
            [
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 0, CashFlowCategory.Sponsors, 100),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 1, CashFlowCategory.Expenses, 30),
                RunStateFixtures.Subscription(1, 2),
            ],
            state.Ledger);
        Assert.Equal(1_572, state.MoneyPln);
    }

    [Fact]
    public void SubscriptionIsRecordedEvenAtZeroPln()
    {
        ValidatedCatalog catalog = DraftCatalog.SingleAction(
            RunStateFixtures.Parameters with { StartingViewers = 9 }, costPln: 0, viewersDelta: 0, dramaDelta: 0);

        RunState state = Plan(Start(catalog), catalog, TestAction);

        Assert.Equal([RunStateFixtures.Subscription(1, 0)], state.Ledger);
    }

    [Theory]
    [InlineData(3, -10, 0, -3)]
    [InlineData(20, 15, 35, 15)]
    public void ViewersStayAtOrAboveZero(int startingViewers, int outcomeDelta, int expectedViewers, int expectedAppliedDelta)
    {
        ValidatedCatalog catalog = DraftCatalog.SingleAction(
            RunStateFixtures.Parameters with { StartingViewers = startingViewers }, costPln: 0, viewersDelta: outcomeDelta, dramaDelta: 0);

        RunState state = Plan(Start(catalog), catalog, TestAction);

        Assert.Equal(expectedViewers, state.Viewers);
        Assert.Equal(expectedAppliedDelta, state.History[0].Action.ViewersDelta);
    }

    [Theory]
    [InlineData(95, 12, 100, 5)]
    [InlineData(2, -5, 0, -2)]
    [InlineData(50, -20, 30, -20)]
    public void DramaStaysWithinZeroToOneHundred(int startingDrama, int outcomeDelta, int expectedDrama, int expectedAppliedDelta)
    {
        ValidatedCatalog catalog = DraftCatalog.SingleAction(
            RunStateFixtures.Parameters with { StartingDrama = startingDrama }, costPln: 0, viewersDelta: 0, dramaDelta: outcomeDelta);

        RunState state = Plan(Start(catalog), catalog, TestAction);

        Assert.Equal(expectedDrama, state.Drama);
        Assert.Equal(expectedAppliedDelta, state.History[0].Action.DramaDelta);
    }

    [Fact]
    public void OutcomeAndSubscriptionRescueADebtFundedCost()
    {
        // -950 - 100 = -1,050 after the cost; +80 donations and 6 PLN of subscriptions close the step at -964.
        ValidatedCatalog catalog = DraftCatalog.SingleAction(
            RunStateFixtures.InDebtParameters, costPln: 100, viewersDelta: 45, dramaDelta: 0, DraftCatalog.Donations(80));

        RunState state = Plan(Start(catalog), catalog, TestAction);

        Assert.Equal(RunStatus.Active, state.Status);
        Assert.Equal(-964, state.MoneyPln);
        Assert.Equal(2, state.Week);
    }

    [Theory]
    [InlineData(50, RunStatus.Bankrupt, -1_000)]
    [InlineData(49, RunStatus.Active, -999)]
    public void StepClosingAtTheThresholdBankruptsTheRun(long costPln, RunStatus expectedStatus, long expectedMoneyPln)
    {
        ValidatedCatalog catalog = DraftCatalog.SingleAction(
            RunStateFixtures.InDebtParameters with { StartingViewers = 0 }, costPln, viewersDelta: 0, dramaDelta: 0);

        RunState state = Plan(Start(catalog), catalog, TestAction);

        Assert.Equal(expectedStatus, state.Status);
        Assert.Equal(expectedMoneyPln, state.MoneyPln);
        Assert.Equal(expectedStatus == RunStatus.Bankrupt ? new RunEnding(RunEndingKind.Bankrupt, 1, null) : null, state.Ending);
        Assert.Equal(expectedStatus == RunStatus.Bankrupt ? 1 : 2, state.Week);
    }

    [Fact]
    public void BankruptcyTakesPriorityInTheFinalWeek()
    {
        ValidatedCatalog catalog = DraftCatalog.SingleAction(
            RunStateFixtures.InDebtParameters with { RunLengthWeeks = 1, StartingViewers = 0 }, costPln: 50, viewersDelta: 0, dramaDelta: 0);

        RunState state = Plan(Start(catalog), catalog, TestAction);

        Assert.Equal(RunStatus.Bankrupt, state.Status);
        Assert.Equal(new RunEnding(RunEndingKind.Bankrupt, 1, null), state.Ending);
    }

    [Fact]
    public void FinalWeekCompletesTheRun()
    {
        GameParameters oneWeek = RunStateFixtures.Parameters with { RunLengthWeeks = 1 };

        RunState state = Plan(Start(DraftCatalog.Validated(oneWeek)), DraftCatalog.Validated(oneWeek), "regular_stream");

        Assert.Equal(RunStatus.Completed, state.Status);
        Assert.Equal(1, state.Week);
        Assert.Equal(new RunEnding(RunEndingKind.Completed, 1, null), state.Ending);
        Assert.Single(state.History);
    }

    [Theory]
    [MemberData(nameof(GuardNames))]
    public void GuardRejectsTheCommandWithoutChangingTheRun(string name)
    {
        (ValidatedRunState run, ValidatedCatalog catalog, string actionId, string code) = Guards[name]();
        int ledgerCount = run.State.Ledger.Count;
        int historyCount = run.State.History.Count;

        WeekPlanResult result = WeekEngine.PlanWeek(run, catalog, actionId);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal(code, result.ErrorCode);
        Assert.Equal((ledgerCount, historyCount), (run.State.Ledger.Count, run.State.History.Count));
    }

    [Fact]
    public void PlanningNeitherMutatesNorKeepsTheInputLists()
    {
        ValidatedRunState run = Validate(RunStateFixtures.Active(), RunStateFixtures.Parameters);
        CashFlowEntry[] ledgerBefore = [.. run.State.Ledger];
        WeekRecord[] historyBefore = [.. run.State.History];

        RunState next = Plan(run, DraftCatalog.Validated(), "quiet_week");

        Assert.Equal(ledgerBefore, run.State.Ledger);
        Assert.Equal(historyBefore, run.State.History);
        Assert.NotSame(run.State.Ledger, next.Ledger);
        Assert.NotSame(run.State.History, next.History);
    }

    [Fact]
    public void SameInputGivesTheSameResult()
    {
        ValidatedRunState run = Validate(RunStateFixtures.Active(), RunStateFixtures.Parameters);
        ValidatedCatalog catalog = DraftCatalog.Validated();

        RunState first = Plan(run, catalog, "provocative_stunt");
        RunState second = Plan(run, catalog, "provocative_stunt");

        RunStateAssert.Equivalent(first, second);
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(2_026UL)]
    [InlineData(ulong.MaxValue)]
    public void EngineConsumesExactlyOneRollFromTheRunGenerator(ulong seed)
    {
        ValidatedCatalog catalog = DraftCatalog.Validated();
        ValidatedRunState run = Start(catalog, seed);
        Pcg32 independent = new(run.State.Rng);
        int expectedRoll = independent.NextRoll();
        string expectedOutcome = OutcomeTable.Select(DraftCatalog.WeeklyActions[1].Outcomes, expectedRoll).OutcomeId;

        RunState state = Plan(run, catalog, "provocative_stunt");

        Assert.Equal(expectedRoll, state.History[0].Action.Roll);
        Assert.Equal(expectedOutcome, state.History[0].Action.OutcomeId);
        Assert.Equal(independent.ToState(), state.Rng);
    }

    [Fact]
    public void NullArgumentsAreRejected()
    {
        ValidatedCatalog catalog = DraftCatalog.Validated();
        ValidatedRunState run = Start(catalog);

        Assert.Throws<ArgumentNullException>(() => WeekEngine.PlanWeek(null!, catalog, "regular_stream"));
        Assert.Throws<ArgumentNullException>(() => WeekEngine.PlanWeek(run, null!, "regular_stream"));
        Assert.Throws<ArgumentNullException>(() => WeekEngine.PlanWeek(run, catalog, null!));
    }

    [Fact]
    public void AllListsEveryDeclaredCode()
    {
        string[] declared =
        [
            .. typeof(WeekPlanErrorCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(static field => field.IsLiteral)
                .Select(static field => (string)field.GetRawConstantValue()!),
        ];

        Assert.Equal(declared.Order(StringComparer.Ordinal), WeekPlanErrorCodes.All.Order(StringComparer.Ordinal));
    }

    /// <summary>Plans one week, requires success, and checks that the result passes the run-state validator.</summary>
    private static RunState Plan(ValidatedRunState run, ValidatedCatalog catalog, string actionId)
    {
        WeekPlanResult result = WeekEngine.PlanWeek(run, catalog, actionId);
        Assert.True(result.IsSuccess, result.ErrorCode);
        RunStateValidation validation = RunStateValidator.Validate(result.Value.State, catalog.Parameters);
        Assert.True(validation.IsValid, string.Join(", ", validation.Errors));
        return result.Value.State;
    }

    private static ValidatedRunState Start(ValidatedCatalog catalog, ulong seed = 42)
    {
        RunStateValidation start = RunStarter.Start(catalog, RunStateFixtures.RunId, "NeonBorsuk", seed, stream: 54);
        Assert.True(start.IsValid, string.Join(", ", start.Errors));
        return start.Value;
    }

    private static ValidatedRunState Validate(RunState state, GameParameters parameters)
    {
        RunStateValidation validation = RunStateValidator.Validate(state, parameters);
        Assert.True(validation.IsValid, string.Join(", ", validation.Errors));
        return validation.Value;
    }

    private static ValidatedCatalog Revalidate(GameCatalog catalog)
    {
        CatalogValidation validation = GameCatalogValidator.Validate(catalog);
        Assert.True(validation.IsValid, string.Join(", ", validation.Errors));
        return validation.Value;
    }

    private static (ValidatedRunState, ValidatedCatalog, string, string) OutOfRange(
        GameParameters parameters, int viewersDelta, params CashFlowAmount[] cashFlows)
    {
        ValidatedCatalog catalog = DraftCatalog.SingleAction(parameters, costPln: 0, viewersDelta, dramaDelta: 0, cashFlows);
        return (Start(catalog), catalog, TestAction, WeekPlanErrorCodes.ValueOutOfRange);
    }
}
```

Create `tests/Domain.Tests/Engine/FullCareerTests.cs`:

```csharp
using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Engine;
using PolskiStreamerSymulatorApp.Domain.Runs;
using PolskiStreamerSymulatorApp.Domain.Tests.Catalog;
using PolskiStreamerSymulatorApp.Domain.Tests.Runs;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Engine;

public sealed class FullCareerTests
{
    private static readonly string[] Rotation = ["regular_stream", "sponsor_pitch", "provocative_stunt", "quiet_week"];

    [Fact]
    public void FiftyTwoWeekCareerStaysValidAndReconcilesEveryWeek()
    {
        ValidatedCatalog catalog = DraftCatalog.Validated();
        RunStateValidation start = RunStarter.Start(catalog, RunStateFixtures.RunId, "NeonBorsuk", seed: 2_026, stream: 10);
        Assert.True(start.IsValid);
        ValidatedRunState run = start.Value;

        while (run.State.Status == RunStatus.Active)
        {
            string actionId = Rotation[(run.State.Week - 1) % Rotation.Length];
            WeekPlanResult result = WeekEngine.PlanWeek(run, catalog, actionId);
            Assert.True(result.IsSuccess, result.ErrorCode);
            RunStateValidation validation = RunStateValidator.Validate(result.Value.State, catalog.Parameters);
            Assert.True(validation.IsValid, string.Join(", ", validation.Errors));
            run = result.Value;
        }

        Assert.Equal(RunStatus.Completed, run.State.Status);
        Assert.Equal(52, run.State.History.Count);
        long expectedOpeningPln = catalog.Parameters.StartingMoneyPln;
        foreach (WeekRecord record in run.State.History)
        {
            WeekCashSummary summary = WeekCashSummary.For(run, record.Week);
            Assert.Equal(expectedOpeningPln, summary.OpeningMoneyPln);
            Assert.Equal(
                summary.OpeningMoneyPln + summary.SponsorsPln + summary.DonationsPln + summary.SubscriptionsPln - summary.ExpensesPln,
                summary.ClosingMoneyPln);
            expectedOpeningPln = summary.ClosingMoneyPln;
        }

        Assert.Equal(run.State.MoneyPln, expectedOpeningPln);
    }

    [Fact]
    public void BankruptRunStopsAdvancing()
    {
        GameParameters parameters = RunStateFixtures.InDebtParameters with { StartingViewers = 0 };
        ValidatedCatalog catalog = DraftCatalog.SingleAction(parameters, costPln: 60, viewersDelta: 0, dramaDelta: 0);
        RunStateValidation start = RunStarter.Start(catalog, RunStateFixtures.RunId, "NeonBorsuk", seed: 1, stream: 1);
        Assert.True(start.IsValid);

        WeekPlanResult first = WeekEngine.PlanWeek(start.Value, catalog, "test_action");
        Assert.True(first.IsSuccess);
        WeekPlanResult second = WeekEngine.PlanWeek(first.Value, catalog, "test_action");

        Assert.Equal(RunStatus.Bankrupt, first.Value.State.Status);
        Assert.Equal(WeekPlanErrorCodes.RunFinished, second.ErrorCode);
    }
}
```

- [ ] **Step 3: Run the tests and see them fail**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj`

Expected: the build fails with error CS0103 or CS0246, because `WeekEngine`, `WeekPlanResult`, and `WeekPlanErrorCodes` do not exist yet.

- [ ] **Step 4: Implement the engine**

Create `src/Domain/Engine/WeekPlanResult.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;
using PolskiStreamerSymulatorApp.Domain.Runs;

namespace PolskiStreamerSymulatorApp.Domain.Engine;

/// <summary>
/// Outcome of planning a week: either the next validated run state or one stable error code from WeekPlanErrorCodes.
/// </summary>
public sealed class WeekPlanResult
{
    private WeekPlanResult(ValidatedRunState? value, string? errorCode)
    {
        Value = value;
        ErrorCode = errorCode;
    }

    public ValidatedRunState? Value { get; }

    public string? ErrorCode { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(ErrorCode))]
    public bool IsSuccess => Value is not null;

    internal static WeekPlanResult Success(ValidatedRunState value)
    {
        return new WeekPlanResult(value, null);
    }

    internal static WeekPlanResult Failure(string errorCode)
    {
        return new WeekPlanResult(null, errorCode);
    }
}
```

Create `src/Domain/Engine/WeekPlanErrorCodes.cs`:

```csharp
namespace PolskiStreamerSymulatorApp.Domain.Engine;

/// <summary>
/// Stable codes for a week that cannot be planned. The S1 design spec states each condition and the problem code it maps to.
/// </summary>
public static class WeekPlanErrorCodes
{
    public const string CatalogMismatch = "catalog_mismatch";
    public const string WeekPending = "week_pending";
    public const string RunFinished = "run_finished";
    public const string UnknownAction = "unknown_action";
    public const string ValueOutOfRange = "value_out_of_range";

    public static IReadOnlyList<string> All { get; } =
    [
        CatalogMismatch,
        WeekPending,
        RunFinished,
        UnknownAction,
        ValueOutOfRange,
    ];
}
```

Create `src/Domain/Engine/WeekEngine.cs`:

```csharp
using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Randomness;
using PolskiStreamerSymulatorApp.Domain.Runs;

namespace PolskiStreamerSymulatorApp.Domain.Engine;

/// <summary>
/// Plays one week of a validated run against its pinned, validated catalogue. The same inputs always give the same result,
/// an error leaves no partial change, and the input lists are never mutated or kept.
/// Until S3 adds events, a week ends straight after its weekly action step.
/// </summary>
public static class WeekEngine
{
    private const int ActionStep = 0;

    public static WeekPlanResult PlanWeek(ValidatedRunState run, ValidatedCatalog catalog, string actionId)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(actionId);

        RunState state = run.State;
        if (state.CatalogVersion != catalog.Version || run.Parameters != catalog.Parameters)
        {
            return WeekPlanResult.Failure(WeekPlanErrorCodes.CatalogMismatch);
        }

        if (state.Status == RunStatus.PendingWeek)
        {
            return WeekPlanResult.Failure(WeekPlanErrorCodes.WeekPending);
        }

        if (state.Status != RunStatus.Active)
        {
            return WeekPlanResult.Failure(WeekPlanErrorCodes.RunFinished);
        }

        if (!catalog.TryGetAction(actionId, out WeeklyActionDefinition? action))
        {
            return WeekPlanResult.Failure(WeekPlanErrorCodes.UnknownAction);
        }

        GameParameters parameters = catalog.Parameters;
        int week = state.Week;
        List<CashFlowEntry> entries = [];

        // The guaranteed cost is paid before the roll, even with no cash on hand.
        if (action.GuaranteedCostPln > 0)
        {
            entries.Add(new CashFlowEntry(
                week, ActionStep, CashFlowSource.WeeklyActionCost, Ordinal: 0, CashFlowCategory.Expenses, action.GuaranteedCostPln));
        }

        Pcg32 generator = new(state.Rng);
        int roll = generator.NextRoll();
        WeeklyActionOutcome outcome = OutcomeTable.Select(action.Outcomes, roll);
        for (int ordinal = 0; ordinal < outcome.CashFlows.Count; ordinal++)
        {
            CashFlowAmount cashFlow = outcome.CashFlows[ordinal];
            entries.Add(new CashFlowEntry(
                week, ActionStep, CashFlowSource.WeeklyActionOutcome, ordinal, cashFlow.Category, cashFlow.AmountPln));
        }

        long viewers = Math.Max(0L, (long)state.Viewers + outcome.ViewersDelta);
        if (viewers > int.MaxValue)
        {
            return WeekPlanResult.Failure(WeekPlanErrorCodes.ValueOutOfRange);
        }

        int drama = Math.Clamp(state.Drama + outcome.DramaDelta, 0, GameParametersValidator.MaxDrama);
        long subscription = SubscriptionSettlement.WeeklyAmountPln((int)viewers, parameters.SubscriptionViewersPerPln);
        entries.Add(new CashFlowEntry(
            week, ActionStep, CashFlowSource.Subscription, Ordinal: 0, CashFlowCategory.Subscriptions, subscription));

        // The whole step reconciles before the bankruptcy check, so the outcome and the subscription can rescue the run.
        Int128 money = state.MoneyPln;
        foreach (CashFlowEntry entry in entries)
        {
            money += entry.SignedAmountPln;
        }

        if (money < long.MinValue || money > long.MaxValue)
        {
            return WeekPlanResult.Failure(WeekPlanErrorCodes.ValueOutOfRange);
        }

        ActionResolution resolution = new(
            action.ActionId, roll, outcome.OutcomeId, (int)viewers - state.Viewers, drama - state.Drama);
        WeekRecord record = new(week, resolution, EncounterRolls: [], Events: []);
        RunState next = CompleteWeek(state, parameters, record, entries, (long)money, (int)viewers, drama, generator.ToState());
        return WeekPlanResult.Success(new ValidatedRunState(next, parameters));
    }

    /// <summary>
    /// Records a finished week and decides what comes next. S3 inserts the event phase before this step.
    /// </summary>
    private static RunState CompleteWeek(
        RunState state,
        GameParameters parameters,
        WeekRecord record,
        IReadOnlyList<CashFlowEntry> newEntries,
        long money,
        int viewers,
        int drama,
        RngState rng)
    {
        RunStatus status;
        int nextWeek;
        RunEnding? ending;
        if (money <= parameters.BankruptcyThresholdPln)
        {
            // Bankruptcy takes priority over ordinary completion, also in the final week.
            status = RunStatus.Bankrupt;
            nextWeek = record.Week;
            ending = new RunEnding(RunEndingKind.Bankrupt, record.Week, ReasonCode: null);
        }
        else if (record.Week == parameters.RunLengthWeeks)
        {
            status = RunStatus.Completed;
            nextWeek = record.Week;
            ending = new RunEnding(RunEndingKind.Completed, record.Week, ReasonCode: null);
        }
        else
        {
            status = RunStatus.Active;
            nextWeek = record.Week + 1;
            ending = null;
        }

        return state with
        {
            Week = nextWeek,
            Status = status,
            MoneyPln = money,
            Viewers = viewers,
            Drama = drama,
            Rng = rng,
            Ledger = [.. state.Ledger, .. newEntries],
            History = [.. state.History, record],
            CurrentWeek = null,
            Flags = [.. state.Flags],
            Ending = ending,
        };
    }
}
```

- [ ] **Step 5: Run the tests and see them pass**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj`

Expected: `Passed!` with `total: 215` and `failed: 0`.

- [ ] **Step 6: Commit**

```bash
cd .. && git add PolskiStreamerSymulatorApp/src/Domain/Engine PolskiStreamerSymulatorApp/tests/Domain.Tests/Runs/RunStateAssert.cs PolskiStreamerSymulatorApp/tests/Domain.Tests/Engine && git commit -m "Add the weekly action engine" -m "<your Co-Authored-By trailer>"
```

---

### Task 5: Action-only balance sweep

**Files:**
- Create: `PolskiStreamerSymulatorApp/tests/Domain.Tests/Balance/ActionOnlySweep.cs`, `ActionOnlySweepTests.cs`

**Interfaces:**
- Consumes: `WeekEngine.PlanWeek`, `RunStarter.Start`, `DraftCatalog.Validated()`, `RunStateValidator.Validate`, `Pcg32`, and `RunStateFixtures.RunId`.
- Produces:
  - `ActionOnlySweep` with `SeedsPerStrategy`, `CheckpointWeeks`, `Strategies`, `Run`, and `Render`;
  - `SweepStrategy` and `StrategyResult`;
  - the explicit test `WritesTheActionOnlyBalanceReport`, which writes `artifacts/balance/actions-only.md`.

This task adds only test code. Its check is the explicit run in Step 3: it must succeed, and the default run must skip it.

- [ ] **Step 1: Add the sweep and its explicit test**

Create `tests/Domain.Tests/Balance/ActionOnlySweep.cs`:

```csharp
using System.Globalization;
using System.Text;
using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Engine;
using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Randomness;
using PolskiStreamerSymulatorApp.Domain.Runs;
using PolskiStreamerSymulatorApp.Domain.Tests.Runs;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Balance;

/// <summary>
/// A fixed way of choosing each week's action. The policy generator is separate from the run's generator,
/// so a strategy never consumes gameplay rolls.
/// </summary>
internal sealed record SweepStrategy(string Name, Func<ValidatedCatalog, Pcg32, string> ChooseAction);

/// <summary>
/// The statistics one strategy produced over many seeded careers.
/// </summary>
internal sealed class StrategyResult(string name)
{
    public string Name { get; } = name;

    public List<ValidatedRunState> FinalRuns { get; } = [];

    public List<int> BankruptcyWeeks { get; } = [];

    public Dictionary<int, List<long>> MoneyAtCheckpoint { get; } = [];

    public Dictionary<int, List<int>> ViewersAtCheckpoint { get; } = [];

    public int[] DramaBandWeeks { get; } = new int[3];

    public long[] CategoryTotalsPln { get; } = new long[4];

    public int PlayedWeeks { get; set; }
}

/// <summary>
/// Plays action-only careers for several strategies and renders the balance report. Numbers come from careers without events.
/// </summary>
internal static class ActionOnlySweep
{
    public const int SeedsPerStrategy = 1_000;
    public const ulong GameplayStream = 1;
    public const ulong PolicyStream = 2;

    public static readonly int[] CheckpointWeeks = [1, 13, 26, 52];

    public static IReadOnlyList<SweepStrategy> Strategies(ValidatedCatalog catalog)
    {
        List<SweepStrategy> strategies =
        [
            .. catalog.WeeklyActions.Select(static action => new SweepStrategy($"always {action.ActionId}", (_, _) => action.ActionId)),
        ];
        strategies.Add(new SweepStrategy(
            "uniform random",
            static (catalog, policy) => catalog.WeeklyActions[(int)policy.NextBounded((uint)catalog.WeeklyActions.Count)].ActionId));
        return strategies;
    }

    public static StrategyResult Run(ValidatedCatalog catalog, SweepStrategy strategy, int seeds)
    {
        StrategyResult result = new(strategy.Name);
        foreach (int week in CheckpointWeeks)
        {
            result.MoneyAtCheckpoint[week] = [];
            result.ViewersAtCheckpoint[week] = [];
        }

        for (ulong seed = 0; seed < (ulong)seeds; seed++)
        {
            RunStateValidation start = RunStarter.Start(catalog, RunStateFixtures.RunId, "Sweep", seed, GameplayStream);
            if (!start.IsValid)
            {
                throw new InvalidOperationException(string.Join(", ", start.Errors));
            }

            ValidatedRunState run = start.Value;
            Pcg32 policy = new(Pcg32.Seed(seed, PolicyStream));
            while (run.State.Status == RunStatus.Active)
            {
                WeekPlanResult planned = WeekEngine.PlanWeek(run, catalog, strategy.ChooseAction(catalog, policy));
                if (!planned.IsSuccess)
                {
                    throw new InvalidOperationException(planned.ErrorCode);
                }

                run = planned.Value;
                Record(result, catalog.Parameters, run.State);
            }

            result.FinalRuns.Add(run);
        }

        return result;
    }

    public static string Render(ValidatedCatalog catalog, IReadOnlyList<StrategyResult> results)
    {
        StringBuilder report = new();
        report.AppendLine("# Action-only balance sweep");
        report.AppendLine();
        report.AppendLine(Invariant(
            $"Careers made only of weekly actions, without events, so these numbers are no balance verdict. Catalogue version {catalog.Version}, {SeedsPerStrategy:N0} seeds per strategy, run length {catalog.Parameters.RunLengthWeeks} weeks. Money and viewer percentiles count only the runs still playing, or just completed, at that week."));
        foreach (StrategyResult result in results)
        {
            int runs = result.FinalRuns.Count;
            int bankrupt = result.BankruptcyWeeks.Count;
            string medianWeek = bankrupt == 0 ? "none" : Percentile(result.BankruptcyWeeks.ConvertAll(static week => (long)week), 50).ToString(CultureInfo.InvariantCulture);
            int played = Math.Max(1, result.PlayedWeeks);
            report.AppendLine();
            report.AppendLine(Invariant($"## {result.Name}"));
            report.AppendLine();
            report.AppendLine(Invariant($"- Bankrupt runs: {100.0 * bankrupt / runs:F1}% (median bankruptcy week: {medianWeek})"));
            report.AppendLine(Invariant(
                $"- Drama band of played weeks: calm {100.0 * result.DramaBandWeeks[0] / played:F1}%, middle {100.0 * result.DramaBandWeeks[1] / played:F1}%, high {100.0 * result.DramaBandWeeks[2] / played:F1}%"));
            report.AppendLine(Invariant(
                $"- Average per played week: sponsors {(double)result.CategoryTotalsPln[0] / played:F1} PLN, donations {(double)result.CategoryTotalsPln[1] / played:F1} PLN, subscriptions {(double)result.CategoryTotalsPln[2] / played:F1} PLN, expenses {(double)result.CategoryTotalsPln[3] / played:F1} PLN"));
            report.AppendLine();
            report.AppendLine("| Week | Runs | Money p10 | Money p50 | Money p90 | Viewers p10 | Viewers p50 | Viewers p90 |");
            report.AppendLine("| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
            foreach (int week in CheckpointWeeks)
            {
                List<long> money = result.MoneyAtCheckpoint[week];
                List<long> viewers = result.ViewersAtCheckpoint[week].ConvertAll(static count => (long)count);
                report.AppendLine(money.Count == 0
                    ? Invariant($"| {week} | 0 | - | - | - | - | - | - |")
                    : Invariant(
                        $"| {week} | {money.Count} | {Percentile(money, 10)} | {Percentile(money, 50)} | {Percentile(money, 90)} | {Percentile(viewers, 10)} | {Percentile(viewers, 50)} | {Percentile(viewers, 90)} |"));
            }
        }

        return report.ToString();
    }

    private static void Record(StrategyResult result, GameParameters parameters, RunState state)
    {
        WeekRecord played = state.History[^1];
        result.PlayedWeeks++;
        int band = state.Drama <= parameters.CalmDramaMax ? 0 : state.Drama <= parameters.MiddleDramaMax ? 1 : 2;
        result.DramaBandWeeks[band]++;
        foreach (CashFlowEntry entry in state.Ledger.Where(entry => entry.Week == played.Week))
        {
            result.CategoryTotalsPln[(int)entry.Category] += entry.AmountPln;
        }

        if (state.Status == RunStatus.Bankrupt)
        {
            result.BankruptcyWeeks.Add(played.Week);
            return;
        }

        if (result.MoneyAtCheckpoint.TryGetValue(played.Week, out List<long>? money))
        {
            money.Add(state.MoneyPln);
            result.ViewersAtCheckpoint[played.Week].Add(state.Viewers);
        }
    }

    /// <summary>Nearest-rank percentile of a non-empty list.</summary>
    private static long Percentile(List<long> values, int percent)
    {
        long[] sorted = [.. values.Order()];
        int rank = (int)Math.Ceiling(percent / 100.0 * sorted.Length);
        return sorted[Math.Max(rank, 1) - 1];
    }

    private static string Invariant(FormattableString text)
    {
        return text.ToString(CultureInfo.InvariantCulture);
    }
}
```

Create `tests/Domain.Tests/Balance/ActionOnlySweepTests.cs`:

```csharp
using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Runs;
using PolskiStreamerSymulatorApp.Domain.Tests.Catalog;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Balance;

public sealed class ActionOnlySweepTests
{
    /// <summary>
    /// Runs only on demand: dotnet test --project tests/Domain.Tests/Domain.Tests.csproj --explicit only
    /// </summary>
    [Fact(Explicit = true)]
    public void WritesTheActionOnlyBalanceReport()
    {
        ValidatedCatalog catalog = DraftCatalog.Validated();
        List<StrategyResult> results = [];
        foreach (SweepStrategy strategy in ActionOnlySweep.Strategies(catalog))
        {
            StrategyResult result = ActionOnlySweep.Run(catalog, strategy, ActionOnlySweep.SeedsPerStrategy);
            Assert.Equal(ActionOnlySweep.SeedsPerStrategy, result.FinalRuns.Count);
            Assert.All(result.FinalRuns, run =>
            {
                Assert.NotEqual(RunStatus.Active, run.State.Status);
                RunStateValidation validation = RunStateValidator.Validate(run.State, catalog.Parameters);
                Assert.True(validation.IsValid, string.Join(", ", validation.Errors));
            });
            results.Add(result);
        }

        string report = ActionOnlySweep.Render(catalog, results);
        string path = ReportPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, report);
        TestContext.Current.TestOutputHelper?.WriteLine(report);
        TestContext.Current.TestOutputHelper?.WriteLine($"Report written to {path}");
    }

    /// <summary>The report goes to artifacts/balance/, found by walking up from the test's output folder.</summary>
    private static string ReportPath()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !string.Equals(directory.Name, "artifacts", StringComparison.OrdinalIgnoreCase))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory?.FullName ?? AppContext.BaseDirectory, "balance", "actions-only.md");
    }
}
```

- [ ] **Step 2: Confirm that the default run skips the sweep**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj`

Expected: `total: 216`, `failed: 0`, `succeeded: 215`, `skipped: 1`.

- [ ] **Step 3: Run the sweep on demand**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj --explicit only`

Expected:
- `failed: 0`, `succeeded: 1`, `skipped: 215`, in a few seconds;
- `artifacts/balance/actions-only.md` exists;
- the file starts with `# Action-only balance sweep`;
- it has five `## ` sections: `always regular_stream`, `always provocative_stunt`, `always sponsor_pitch`, `always quiet_week`, and `uniform random`.

Check it with:

```bash
head -3 artifacts/balance/actions-only.md && grep "^## " artifacts/balance/actions-only.md && git status --short
```

`git status --short` must not list the report, because `artifacts` is git-ignored.

- [ ] **Step 4: Commit**

```bash
cd .. && git add PolskiStreamerSymulatorApp/tests/Domain.Tests/Balance && git commit -m "Add the action-only balance sweep" -m "<your Co-Authored-By trailer>"
```

---

### Task 6: Documentation and final verification

**Files:**
- Modify: `docs/technical-design.md`, `docs/balance.md`, `docs/event-system.md`, `docs/testing-strategy.md`, `docs/decisions.md`, `docs/delivery-plan.md`, `docs/superpowers/specs/2026-10-08-s1-weekly-action-engine-design.md`

**Interfaces:**
- Consumes: the types, codes, and commands from Tasks 1 to 5.
- Produces: documentation that matches the code.

Every replacement below is exact, and each target text occurs exactly once in its file.

- [ ] **Step 1: Technical design**

In `docs/technical-design.md`, replace

```text
**Problem codes.** `ProblemCodes` in `Contracts` lists the stable codes that API Problem Details responses use: `invalid_request`, `invalid_state`, `invalid_choice`, `invalid_streamer_name`, `incompatible_save`, `catalog_version_unavailable`, and `run_finished`.
```

with

```text
**Problem codes.** `ProblemCodes` in `Contracts` lists the stable codes that API Problem Details responses use: `invalid_request`, `invalid_state`, `invalid_choice`, `invalid_streamer_name`, `incompatible_save`, `catalog_version_unavailable`, and `run_finished`.

### Weekly action engine

S1 implements the weekly action step in `Domain` (`Catalog`, `Engine`, `Runs`). The design record is `docs/superpowers/specs/2026-10-08-s1-weekly-action-engine-design.md`.

**Validated catalogue.** `GameCatalogValidator.Validate` checks a `GameCatalog`, which holds the version, the parameters, and the enabled weekly actions in catalogue order. It returns every error with a stable code and a path, or a `ValidatedCatalog`, which only the validator creates, from its own copies of the lists. It checks what the engine relies on: stable and unique IDs, at least one action with at least one outcome, chances from 0 to 10,000 that sum to exactly 10,000, cash flows that are sponsors, donations, or expenses, and the engine limits. Publication policy stays with S2, which runs the same validator first.

**Planning a week.** `WeekEngine.PlanWeek` takes a `ValidatedRunState`, the `ValidatedCatalog` of its pinned version, and an action ID. It returns the next `ValidatedRunState`, or one error code: `catalog_mismatch`, `week_pending`, `run_finished`, `unknown_action`, or `value_out_of_range`. It records the guaranteed cost, draws one roll, applies the outcome's cash flows and clamped deltas, and settles the subscription; only then does it check bankruptcy. Until S3 adds events, the week then ends in bankruptcy, in completion at the final week, or with the next active week. `OutcomeTable` selects outcomes by running sum in catalogue order, and `SubscriptionSettlement` holds the rules-version-1 formula.

**Starting and reporting.** `RunStarter.Start` builds the week-1 state from the catalogue's parameters, a run ID, a normalized streamer name, and a seed and stream, and validates it. `WeekCashSummary.For` returns a week's opening money, its four category totals, and its closing money for the weekly report.

**Engine limits.** Catalogue amounts are at most 1,000,000,000 PLN per entry, and viewer changes at most 1,000,000 per outcome. These are arithmetic guards, not balance rules. Only an edited save can reach a week whose money or viewers would not fit their 64-bit and 32-bit types; such a week fails with `value_out_of_range`.
```

- [ ] **Step 2: Balance**

In `docs/balance.md`, replace

```text
Money has no arbitrary upper cap.
```

with

```text
Money has no arbitrary upper cap; the engine only rejects a week whose money or viewers would not fit their 64-bit and 32-bit types, which an ordinary career cannot reach.
```

then replace

```text
Select the outcome whose cumulative interval contains that value.
```

with

```text
Select the outcome whose cumulative interval contains that value: outcomes are taken in catalogue order, and the first one whose running sum of chances exceeds the roll is selected, so an outcome with a chance of 0 is never selected.
```

and replace

```text
Tune exact odds and deltas against these results and human playtests. Keep the same seeded scenarios when comparing revisions.
```

with

```text
Tune exact odds and deltas against these results and human playtests. Keep the same seeded scenarios when comparing revisions.

Since S1, an action-only sweep gives a first look at the weekly actions before events exist. `tests/Domain.Tests/Balance/ActionOnlySweepTests.cs` plays 1,000 seeded 52-week careers for each of five strategies: always each of the four actions, and a uniform random choice. It writes the bankruptcy share, the drama-band share, the average weekly cash per category, and money and viewer percentiles at weeks 1, 13, 26, and 52 to `PolskiStreamerSymulatorApp/artifacts/balance/actions-only.md`. Run it from `PolskiStreamerSymulatorApp/` with `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj --explicit only`. Without events, its numbers are not a balance verdict.
```

- [ ] **Step 3: Event system**

In `docs/event-system.md`, replace

```text
- Keep EF Core migrations separate from content updates.
```

with

```text
- The publish check first runs `GameCatalogValidator` from `Domain`, the same check the weekly engine relies on, and then applies the publication rules above. The catalogue loader passes only enabled weekly actions, ordered by `SortOrder`. It passes each action's outcomes in a stable, persisted order, because outcome order decides which roll selects which outcome. `WeeklyActionOutcome` has no order column yet, so S2 adds one.
- Keep EF Core migrations separate from content updates.
```

- [ ] **Step 4: Testing strategy**

In `docs/testing-strategy.md`, replace

```text
| `tests/Domain.Tests` | `Domain` | Random generator, streamer names, parameter and run-state validation (in place since F2); later fast deterministic rules and property-style invariants |
```

with

```text
| `tests/Domain.Tests` | `Domain` | Random generator, streamer names, parameter and run-state validation (in place since F2); the weekly action engine, catalogue validation, run start, and weekly cash summary (since S1); later fast deterministic rules and property-style invariants |
```

and replace

```text
Balance sweeps can live in `Domain.Tests` under a separate trait and run before release or nightly, so a 1,000-seed test does not slow every edit.
```

with

```text
Balance sweeps live in `Domain.Tests` as explicit xUnit tests, which the default run and CI skip, so a 1,000-seed sweep does not slow every edit. Run them with `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj --explicit only`; the first one, added in S1, writes `artifacts/balance/actions-only.md`.
```

- [ ] **Step 5: Decisions**

In `docs/decisions.md`, add this row directly after the row that starts with `| D-63 |`:

```text
| D-64 | Weekly action engine | Confirmed | The engine accepts only a `ValidatedRunState` and a `ValidatedCatalog`, which `GameCatalogValidator` creates from its own copies of the catalogue lists; S2 runs the same validator before its publication rules. Catalogue amounts are limited to 1,000,000,000 PLN per entry and viewer changes to 1,000,000 per outcome as arithmetic guards, and a week that would overflow money or viewers fails with `value_out_of_range`. Until S3 adds events, a week ends straight after its action step. Details are in `technical-design.md`. |
```

- [ ] **Step 6: Delivery plan and spec status**

In `docs/delivery-plan.md`, replace

```text
starts the production container on every push (D-63).
```

with the text below. Replace `<DATE>` with today's date in ISO form, `YYYY-MM-DD`, the date of this commit; do not copy the spec's date.

```text
starts the production container on every push (D-63). S1 was completed on <DATE>: Domain plays action-only weeks through `WeekEngine.PlanWeek` against a validated catalogue, starts runs, and summarizes weekly cash, and an explicit sweep reports action-only careers (D-64).
```

In `docs/superpowers/specs/2026-10-08-s1-weekly-action-engine-design.md`, replace

```text
| Status | Design approved by the creator in conversation on 2026-10-08; this written spec awaits review. |
```

with

```text
| Status | Approved by the creator on 2026-10-08. |
```

- [ ] **Step 7: Final verification**

Run:

```bash
dotnet build PolskiStreamerSymulatorApp.sln -c Release
dotnet format PolskiStreamerSymulatorApp.sln --verify-no-changes
dotnet test --solution PolskiStreamerSymulatorApp.sln -c Release
dotnet list src/Domain/Domain.csproj reference
```

Expected:
- `0 Warning(s)` and `0 Error(s)`;
- the format check exits with code 0;
- tests `total: 287`, `failed: 0`, `succeeded: 286`, `skipped: 1`;
- Domain reports no project references.

- [ ] **Step 8: Commit**

```bash
cd .. && git add docs/technical-design.md docs/balance.md docs/event-system.md docs/testing-strategy.md docs/decisions.md docs/delivery-plan.md docs/superpowers/specs/2026-10-08-s1-weekly-action-engine-design.md && git commit -m "Document the weekly action engine" -m "<your Co-Authored-By trailer>" && git status --short
```

Expected: the commit succeeds and `git status --short` prints nothing.
