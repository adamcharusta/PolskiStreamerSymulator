# S1 design: weekly action engine and subscription settlement

| Field | Value |
| --- | --- |
| Work package | S1 in the [delivery plan](../../delivery-plan.md) |
| Date | 2026-10-08 |
| Status | Design approved by the creator in conversation on 2026-10-08; this written spec awaits review. |
| Authoritative documents | [Balance](../../balance.md), [game design](../../game-design.md), [event system](../../event-system.md), [technical design](../../technical-design.md), [testing strategy](../../testing-strategy.md) |

This spec records how S1 will be built. The documents above stay authoritative for rules and contracts; this file does not replace them.

## Goal

The pure Domain can play a career made only of weekly actions, from a fresh run to its ending. S1 is done when:

1. The same run state, catalogue, and action produce the same result, including the generator state.
2. Every money change is a ledger entry in one of the four categories. Each week's closing money equals its opening money plus sponsors, donations, and subscriptions, minus expenses.
3. The atomic bankruptcy check holds at its boundary: -999 PLN stays active and -1,000 PLN ends the run. A debt-funded cost that crosses the threshold can be rescued by its own outcome and the subscription settlement.
4. A seeded balance sweep of action-only careers can be run on demand and writes a report.

## Inputs

From the authoritative documents:

- **Action step:** the guaranteed cost, one seeded roll against the outcome table in basis points, categorized cash flows, clamped viewer and drama deltas, then the weekly subscription. The bankruptcy check comes only after all of that (balance.md, "Percentage-roll contract" and "Cash-flow accounting"; game-design.md, weekly loop step 3; event-system.md, dice sequence step 1).
- **Subscription formula:** `floor(max(0, postActionViewers) / SubscriptionViewersPerPln)`. It is recorded once per week, even at 0 PLN.
- **Bankruptcy:** at or below `BankruptcyThresholdPln`. It takes priority over ordinary completion in the final week (D-26, D-56).
- **Four working actions:** `regular_stream`, `provocative_stunt`, `sponsor_pitch`, and `quiet_week`, with the draft numbers in balance.md (D-25).
- **F2 provides:**
  - `RunState` and its week semantics;
  - `RunStateValidator` and `ValidatedRunState`;
  - `Pcg32` with `NextRoll`;
  - `CashFlowEntry` with the step and source scheme;
  - `GameParameters` and `GameParametersValidator`;
  - the shape records `WeeklyActionDefinition`, `WeeklyActionOutcome`, and `CashFlowAmount`.
- **F2 carry-forward:**
  - records compare list members by reference, so tests need a structural comparison;
  - `ValidatedRunState` aliases the caller's lists, so the engine must not keep or mutate input lists.

Chosen by the creator during brainstorming on 2026-10-08:

- **Scope:** the engine and its catalogue validation, plus a pure `StartRun` initial state, a weekly cash summary, and an action-only balance sweep.
- **Sweep runs:** the sweep is an explicit xUnit test, run on demand and never in the default run or CI. It writes a Markdown report and asserts only invariants.
- **Approach:** a validated catalogue token and a single engine entry point.

## Non-goals

- SQLite, EF Core, catalogue loading, and publication rules such as at least two distinct outcomes per choice, translations, and authoring effect limits (S2).
- Events, encounter rolls, responses, flags, and special endings (S3). The engine ends each week straight after the action step until S3 adds the event phase.
- Application handlers, Wolverine, HTTP endpoints, DTO mapping, and the cryptographic seed source (S4).
- The final career score (M2).
- Balance targets. The sweep reports numbers and does not judge them.

## Approaches considered

1. **A validated catalogue token and one engine entry point (chosen).** A catalogue validator returns errors or a `ValidatedCatalog` that only it can create, the same pattern as `ValidatedRunState`. The engine accepts only validated inputs. S2 reuses the validator at publication, and S3 adds events to the catalogue and an event phase inside the same entry point.
2. **The engine validates the chosen action's table on every call.** This needs fewer types, but validation repeats per request, S2 gets no reusable proof of a valid catalogue, and a broken table in another action surfaces only when someone picks it.
3. **Public step functions, composed by Application.** This is flexible for S3, but the order of the atomic step leaks out of Domain, where a caller could check bankruptcy before the subscription and break the debt rescue.

## Design

### Layout

| Project | Folder | Contents |
| --- | --- | --- |
| `Domain` | `Catalog` | `GameCatalog`, `GameCatalogValidator`, `CatalogValidation`, `ValidatedCatalog`, `CatalogError`, `CatalogErrorCodes` |
| `Domain` | `Engine` | `WeekEngine`, `WeekPlanResult`, `WeekPlanErrorCodes`, `OutcomeTable`, `SubscriptionSettlement` |
| `Domain` | `Runs` | `RunStarter` |
| `Domain` | `Ledger` | `WeekCashSummary` |
| `tests/Domain.Tests` | `Catalog`, `Engine`, `Runs`, `Ledger`, `Balance` | Unit tests, a draft catalogue fixture, a structural state comparer, and the explicit balance sweep |

Domain keeps no package or project references.

### Catalogue and its validation

- **`GameCatalog`:** `GameCatalog(int Version, GameParameters Parameters, IReadOnlyList<WeeklyActionDefinition> WeeklyActions)` is the untrusted input shape. S2's loader will build it from a published SQLite version, keeping only enabled actions, ordered by `SortOrder`. The outcomes within each action keep a stable, persisted order.
- **`GameCatalogValidator.Validate(GameCatalog catalog)`:** returns a `CatalogValidation` with `IsValid` and `Errors`, plus `ValidatedCatalog Value` when valid. Each `CatalogError` has a stable `Code` and a `Path`, such as `weeklyActions[1].outcomes[2].chanceBps`. It reports every error it finds. It first validates the parameters with `GameParametersValidator`, and stops with `invalid_game_parameters` if they fail.
- **`ValidatedCatalog`:**
  - It has no public constructor.
  - It exposes `Version`, `Parameters`, `WeeklyActions` in catalogue order, and `TryGetAction(string actionId, out WeeklyActionDefinition action)`.
  - The validator copies the action list, every outcome list, and every cash-flow list into new arrays it owns. A later change to the caller's lists cannot change a validated catalogue.

Rules the engine depends on:

| Code | Rule |
| --- | --- |
| `invalid_catalog_version` | `Version` is at least 1 |
| `invalid_game_parameters` | `GameParametersValidator` returns no errors |
| `no_weekly_actions` | There is at least one weekly action |
| `invalid_stable_id` | Every action ID and outcome ID is a stable ID |
| `duplicate_action_id` | Action IDs are unique within the catalogue |
| `duplicate_outcome_id` | Outcome IDs are unique within their action |
| `cost_out_of_range` | `GuaranteedCostPln` is 0 to `MaxAmountPln` |
| `no_outcomes` | Every action has at least one outcome |
| `chance_out_of_range` | Every `ChanceBps` is 0 to 10,000 |
| `chances_do_not_sum` | An action's `ChanceBps` values sum to exactly 10,000 |
| `invalid_cash_flow_category` | A cash flow is `Sponsors`, `Donations`, or `Expenses`, a defined value that is not `Subscriptions` |
| `cash_flow_amount_out_of_range` | A cash-flow `AmountPln` is 1 to `MaxAmountPln` |
| `viewers_delta_out_of_range` | `ViewersDelta` is between -`MaxViewersDelta` and `MaxViewersDelta` |
| `drama_delta_out_of_range` | `DramaDelta` is -100 to 100 |

Engine limits, as constants on `GameCatalogValidator`:

- `MaxAmountPln = 1_000_000_000`;
- `MaxViewersDelta = 1_000_000`.

These limits guard the engine's arithmetic. They are not balance decisions, and S2's publication rules may be stricter.

### Outcome selection

`OutcomeTable.Select(IReadOnlyList<WeeklyActionOutcome> outcomes, int roll)` walks the outcomes in catalogue order with a running sum of `ChanceBps`. It returns the first outcome whose running sum exceeds the roll, so an outcome with a chance of 0 is never chosen.

- It requires a roll of 0 to 9,999 and a table that sums to 10,000; otherwise it throws `ArgumentOutOfRangeException`. A validated catalogue and `Pcg32.NextRoll` always meet both conditions.
- S3 will reuse the same running-sum rule for event responses, through an overload or a generic form.

For `regular_stream`, rolls 0 to 5,999 select `steady`, 6,000 to 8,999 select `good_chat`, and 9,000 to 9,999 select `slow_evening`.

### Subscription settlement

`SubscriptionSettlement.WeeklyAmountPln(int viewers, int subscriptionViewersPerPln)` returns `floor(max(0, viewers) / subscriptionViewersPerPln)`. The formula belongs to `RulesVersion` 1, and the divisor comes from the pinned parameters.

### `WeekEngine.PlanWeek`

`public static WeekPlanResult PlanWeek(ValidatedRunState run, ValidatedCatalog catalog, string actionId)` returns either a new `ValidatedRunState` or one error code. An error leaves no partial change, and the input is never mutated.

**Guards**, checked in this order before any roll:

| Code | Condition | Problem code S4 maps it to |
| --- | --- | --- |
| `catalog_mismatch` | `run.State.CatalogVersion` differs from `catalog.Version`, or `run.Parameters` differs from `catalog.Parameters` | `invalid_state` |
| `week_pending` | The status is `PendingWeek`; reachable only once S3 exists | `invalid_state` |
| `run_finished` | The status is `Completed`, `Bankrupt`, or `SpecialEnding` | `run_finished` |
| `unknown_action` | `actionId` is not in the catalogue | `invalid_choice` |

Insufficient cash is never an error, because paid actions may use debt.

**Action step**, for the current week `w`:

1. If `GuaranteedCostPln` is above 0, add the entry `(w, step 0, WeeklyActionCost, ordinal 0, Expenses, cost)`.
2. Draw one roll with `Pcg32.NextRoll` from the run's generator, and select the outcome with `OutcomeTable.Select`.
3. For each cash flow `i` of the outcome, add `(w, 0, WeeklyActionOutcome, i, category, amount)`.
4. Apply the deltas with clamping: viewers at least 0, drama 0 to 100. `ActionResolution` records the action ID, the roll, the outcome ID, and the deltas actually applied.
5. Settle the subscription from the post-action viewers, and add `(w, 0, Subscription, 0, Subscriptions, amount)`, even when the amount is 0.
6. New money is the old money plus every signed entry of the step, computed in 128-bit arithmetic.
   - If new money does not fit in `long`, or new viewers exceed `int.MaxValue`, return `value_out_of_range` and no state.
   - A real career cannot approach these bounds; only an edited save can.

**Week completion**, a separate private step. S3 will insert the event phase before it.

- Append `WeekRecord(w, action, [], [])` to the history.
- If new money is at or below `BankruptcyThresholdPln`: the status becomes `Bankrupt`, `Week` stays `w`, and `Ending` is `(Bankrupt, w, null)`. This holds in the final week too.
- Otherwise, if `w` equals `RunLengthWeeks`: `Completed`, with `Ending` `(Completed, w, null)`.
- Otherwise: `Active`, with `Week = w + 1`.
- `Rng` becomes the generator's state after the roll.
- `Flags` stay as they were, and `CurrentWeek` stays null.

**Output.** The new `RunState` gets new lists: the old entries plus the new ones. It is wrapped in `ValidatedRunState` through the internal constructor, without running the validator again. The tests check that every state the engine produces passes `RunStateValidator`.

**Debt rescue example.** Money is -950 PLN and the action is `provocative_stunt` (cost 100 PLN), so money briefly sits at -1,050 PLN.

- The `viral` outcome adds 80 PLN in donations, and the subscription adds its amount, so the step closes above -1,000 PLN and the run stays active.
- A step that closes at exactly -1,000 PLN is bankrupt; one that closes at -999 PLN is not.

### `RunStarter.Start`

`public static RunStateValidation Start(ValidatedCatalog catalog, Guid runId, string streamerName, ulong seed, ulong stream)` builds week 1 in `Active` status:

- `RulesVersion.Current` and `catalog.Version`;
- money, viewers, and drama from the parameters;
- `Rng = Pcg32.Seed(seed, stream)`;
- empty ledger, history, and flags, with no current week or ending.

The name is normalized with `StreamerName.TryNormalize`. If normalization fails, the raw name is kept, so that validation reports it. The state goes through `RunStateValidator.Validate`, so an invalid name or an empty run ID produce the F2 codes `invalid_streamer_name` and `invalid_run_id`. S4 supplies the cryptographic seed and stream.

### `WeekCashSummary`

`public sealed record WeekCashSummary(int Week, long OpeningMoneyPln, long SponsorsPln, long DonationsPln, long SubscriptionsPln, long ExpensesPln, long ClosingMoneyPln)` with `public static WeekCashSummary For(ValidatedRunState run, int week)`.

- **Opening money:** `StartingMoneyPln` plus every signed entry of earlier weeks.
- **Category totals:** the week's entries summed by category.
- **Closing money:** opening + sponsors + donations + subscriptions - expenses.
- **Week range:** the week must have a record in `History` or `CurrentWeek`; otherwise the method throws `ArgumentOutOfRangeException`.
- **Pending week:** for a `PendingWeek` run, the summary covers the week so far.

## Testing

All new code is written test-first, in `tests/Domain.Tests`.

| Area | Tests |
| --- | --- |
| `GameCatalogValidator` | The four draft actions from balance.md pass. Each rule fails on its boundary: sums of 9,999 and 10,001, a chance of -1 and 10,001, a cost of -1 and `MaxAmountPln + 1`, cash amounts of 0 and `MaxAmountPln + 1`, a `Subscriptions` cash flow, an undefined category, drama deltas of ±101, viewer deltas of ±(`MaxViewersDelta + 1`), duplicate and unstable IDs, and empty lists. Several errors are reported together. A validated catalogue does not change when the caller later mutates its input lists. |
| `OutcomeTable` | The `regular_stream` boundaries 0, 5,999, 6,000, 8,999, 9,000, and 9,999. A zero-chance outcome is never selected. An invalid roll throws. |
| `SubscriptionSettlement` | The balance.md table: 0, 20, 100, 250, and 1,000 viewers give 0, 2, 10, 25, and 100 PLN; a divisor of 1; negative viewers give 0 |

The `WeekEngine` tests cover:

- **Ledger entries:** the cost entry exists only when the cost is above 0; there is one subscription entry, including at 0 PLN; outcome cash flows carry their ordinals.
- **Clamping:** viewers 3 with a delta of -10 records -3; drama 95 with +12 records +5.
- **Debt rescue:** the rescue example above.
- **Bankruptcy:** the -1,000 and -999 PLN boundaries; bankruptcy wins in the final week; the final week otherwise completes.
- **Errors:** each guard code, and `value_out_of_range` from an edited state near the type limits.
- **Purity:** the input is not mutated.
- **Determinism:** the same input gives structurally equal output.
- **Known answer:** the engine's roll and outcome match a separate `Pcg32` built from the input generator state.

| Area | Tests |
| --- | --- |
| Full career | One seeded 52-week career with a fixed action sequence. Every intermediate state passes `RunStateValidator`. For every week, the opening money equals the previous week's closing money, and the last closing money equals `MoneyPln`. A one-week run completes at once, and a run that goes bankrupt stops advancing. |
| `RunStarter` | Starting values come from the parameters. The seed and stream are kept. An NFD name with outer spaces is stored in its normalized form. An invalid name and an empty run ID give the F2 codes. |
| `WeekCashSummary` | The balance.md illustration: opening 1,500 PLN, sponsors 200, donations 35, subscriptions 18, and expenses 60 give closing money of 1,693 PLN. A week without a record throws. |

A test helper compares run states structurally: records field by field, and lists element by element.

### Balance sweep

`tests/Domain.Tests/Balance/ActionOnlySweepTests.cs` holds one `[Fact(Explicit = true)]`. It does not run by default, so it does not run in CI either. Run it on demand with:

    dotnet test --project tests/Domain.Tests/Domain.Tests.csproj --explicit only

**What it simulates:**

- the draft catalogue fixture: the four actions and the first published parameters;
- five strategies: always `regular_stream`, always `provocative_stunt`, always `sponsor_pitch`, always `quiet_week`, and a uniform random choice;
- 1,000 seeds per strategy, each run for 52 weeks or until bankruptcy;
- the random strategy draws from its own `Pcg32`, so it never consumes gameplay rolls.

**What the report contains**, per strategy:

- the share of bankrupt runs, and the median bankruptcy week;
- the 10th, 50th, and 90th percentiles of money and viewers at the end of weeks 1, 13, 26, and 52, among the runs still active at that point;
- the share of played weeks in the calm, middle, and high drama bands, using the pinned band limits;
- the average weekly sponsors, donations, subscriptions, and expenses.

**Where it goes:**

- a Markdown file at `artifacts/balance/actions-only.md` under `PolskiStreamerSymulatorApp/`. The test finds the `artifacts` folder by walking up from its output directory. The folder is git-ignored.
- the same text goes to the test output.
- the report's header says the numbers come from action-only careers, without events.

**What it asserts:** only invariants. Every final state passes `RunStateValidator`, and its ledger reconciles. It asserts no balance targets.

## Documentation updates

- **`docs/technical-design.md`:** a "Weekly action engine" subsection under the run-state section. It covers `ValidatedCatalog`, `PlanWeek` with its guards and error codes, the S1 week completion without events, `RunStarter`, `WeekCashSummary`, and the engine limits.
- **`docs/balance.md`:**
  - outcomes are selected in catalogue order by running sum;
  - the engine limits;
  - the action-only sweep and how to run it.
- **`docs/event-system.md`:** S2's publication check calls `GameCatalogValidator`, and outcome order must be persisted deterministically.
- **`docs/testing-strategy.md`:** the explicit balance sweep and its command.
- **`docs/decisions.md`:** D-64 records the validated catalogue token, the engine limits, and S1's week completion without events until S3.
- **`docs/delivery-plan.md`:** S1 progress.

## Risks

- **Action-only numbers are incomplete.** Events will change cash and viewers a lot. The report says so, and R2 owns real balance sweeps.
- **The engine limits are arbitrary.** They prevent overflow and say nothing about balance. Raising them later is a rules change only if saved states could already exceed them, which a real career cannot.
- **S3 changes the end of `PlanWeek`.** Week completion is a separate step, so S3 inserts the event phase before it. Encounter rolls then draw from the same generator after the action roll, which keeps a week's roll order stable.
- **Catalogue order affects rolls.** A different outcome order changes which roll selects which outcome. S2 must persist and load the order exactly; the event-system update states this.
