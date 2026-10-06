# F2 design: run state, randomness, catalogue, and wire contracts

| Field | Value |
| --- | --- |
| Work package | F2 in the [delivery plan](../../delivery-plan.md) |
| Date | 2026-10-06 |
| Status | Proposed, awaiting the creator's review |
| Authoritative documents | [Technical design](../../technical-design.md), [game design](../../game-design.md), [balance](../../balance.md), [event system](../../event-system.md), [streamer name generation](../../streamer-name-generation.md), [testing strategy](../../testing-strategy.md) |

This spec records how F2 will be built. The documents above stay authoritative for rules and contracts, and the implementation updates them.

## Goal

Define the data that every later package exchanges: the career state saved in the browser, the seeded random generator, the cash ledger, the pinned numeric parameters, the weekly-action catalogue shape, terminal reasons, and the HTTP request and response shapes. F2 adds no gameplay behavior; S1 builds the weekly engine on these types. F2 is done when:

1. The domain types cover active, pending-event, completed, bankrupt, and special-ending states, the pinned rules and catalogue versions, the three statistics, and stable IDs for localized text.
2. A domain validator rejects impossible state and hands the engine a validated state only.
3. The wire contracts serialize to one documented JSON format, and deserialization rejects malformed input.
4. The random generator reproduces the published PCG32 reference output.

## Inputs

Stated by the creator and recorded as confirmed in the documentation:

- D-10, D-22, D-27: money, viewers, and drama are the only statistics; every choice rolls against weighted outcomes; money changes only through sponsor, donation, subscription, and expense entries.
- D-07, D-26, D-33, D-55: run length, bankruptcy threshold, event cap, and other numbers are typed parameters pinned by the run's catalogue version.
- D-09, D-58: the career save lives in the browser; the server is stateless and must validate untrusted state.
- D-52, D-54, D-59: narrative flags, per-event repeat policy, and the three special endings with their classifications.
- The technical design's list of run-state fields and its rule that the domain model is rebuilt through validation, never blindly mapped from JSON.

Decisions this spec makes for review:

- **Random generator:** PCG32 (XSH-RR 64/32) by Melissa O'Neill, with the reference bounded draw for rolls from 0 to 9,999.
- **Subscription entry:** every week records exactly one subscription entry, even when it pays 0 PLN; every other ledger entry is strictly positive.
- **Ledger references:** an entry names its week, its step, and its source kind; the action, event, option, and outcome IDs come from the week record it points to instead of being copied into the entry.
- **Encounter history:** derived from the weekly history instead of stored as a second list.
- **Streamer name:** the documented rules plus two additions: at least one letter or digit, and at most 64 UTF-16 code units.
- **Wire format:** strict JSON in which every property is required, enums travel as fixed names, and 64-bit generator values travel as hexadecimal strings.
- **Event definitions** wait for S2 and S3, which design eligibility and persistence; F2 defines only the weekly-action catalogue shape.

## Non-goals

Outcome selection, subscription settlement, and the weekly engine (S1); event definitions, conditions, follow-up gates, and eligibility (S2, S3); EF Core persistence (S2); HTTP endpoints, Wolverine handlers, and Mapster mappings between DTOs and domain types (S4); localized text DTOs (S4, M5); the final score (M2); save slots and file export (M3); FluentValidation, because no Application command exists yet.

## Approaches considered

1. **Immutable domain records plus a validator that issues a validated-state token (recommended).** Domain types are positional records. `RunStateValidator` checks a state against its pinned parameters and returns either errors or a `ValidatedRunState`, which only the validator can create. The engine accepts only the token, so unvalidated client data cannot reach game rules. Wire DTOs in `Contracts` mirror the records. Records suit S1's state-in, state-out engine: they copy with `with`, compare easily in tests, and keep invariants in one validator instead of spread across constructors.
2. **Encapsulated aggregate classes** with private setters and behavior methods. Constructors would enforce invariants, but the engine would need mutation methods for every step, and rebuilding a saved state would need a second construction path that skips or repeats checks. More code for the same guarantees.
3. **One shared type set** used by Domain and on the wire. Fewer types, but `Domain` would reference `Contracts` (or the reverse), breaking the documented project graph, and every domain refactor would change the save format.

## Design

### Layout

| Project | Folder and namespace | Contents |
| --- | --- | --- |
| `Domain` | `Versioning` | `RulesVersion` |
| `Domain` | `Identity` | `StableId`, `StreamerName` |
| `Domain` | `Randomness` | `RngState`, `Pcg32` |
| `Domain` | `Catalog` | `GameParameters`, `GameParametersValidator`, `WeeklyActionDefinition`, `WeeklyActionOutcome`, `CashFlowAmount`, `TerminalReasonDefinition`, `TerminalClassification` |
| `Domain` | `Ledger` | `CashFlowCategory`, `CashFlowSource`, `CashFlowEntry` |
| `Domain` | `Runs` | `RunState` and its parts, `RunStateValidator`, `RunStateValidation`, `ValidatedRunState`, `RunStateError`, `RunStateErrorCodes` |
| `Contracts` | `Runs`, `Setup`, `Problems`, `Localization`, `Serialization` | DTOs, requests and responses, problem codes, locales, the JSON context and converters |
| `tests/Domain.Tests` (new) | | Generator, name, parameter, and run-state validation tests |
| `tests/Server.IntegrationTests` | `Contracts` folder | Wire format tests, because the JSON format is part of the HTTP contract |

Namespaces follow the folder: `PolskiStreamerSymulatorApp.Domain.Runs`, `PolskiStreamerSymulatorApp.Contracts.Runs`, and so on. Domain keeps no package references.

### Versions

- `RulesVersion.Current = 1` in Domain. It names the meaning of the coded algorithms, including the random generator. A state with another value is rejected.
- `SaveSchema.CurrentVersion = 1` in Contracts. It names the JSON shape. Mapping in S4 rejects any other value as an incompatible save before building domain records.
- `CatalogVersion` is the published catalogue's positive integer `VersionId`. The caller supplies the `GameParameters` row of that version to the validator.

### Stable IDs

Action, outcome, event, option, flag, and terminal reason IDs match `^[a-z][a-z0-9_]{0,63}$`: lowercase snake case, 1 to 64 characters, starting with a letter. Every documented ID, such as `regular_stream`, `clip_backlash`, and `permanent_ban`, fits. `StableId.IsValid(string)` is the single check.

### Randomness

- **Algorithm:** PCG32 as published in `pcg_basic.c`: 64-bit state, odd increment `(stream << 1) | 1`, multiplier `6364136223846793005`, XSH-RR output. Seeding follows `pcg32_srandom_r`.
- **Rolls:** `NextRoll()` returns 0 to 9,999 through `pcg32_boundedrand_r` with bound 10,000: discard outputs below `(2^32 - 10000) % 10000 = 7296`, then take the remainder. One roll usually consumes one output and very rarely more; the result is unbiased and deterministic.
- **State:** `RngState(string Algorithm, ulong Seed, ulong Stream, ulong State)` with `Algorithm = "pcg32"`. `Seed` and `Stream` stay in the save so a run can be replayed from its start; `State` is the current position. `Pcg32.Seed(seed, stream)` returns the seeded state; `new Pcg32(state)` resumes it; `ToState()` snapshots it after drawing.
- **Seeding source:** S4's `StartRun` draws seed and stream from a cryptographic random source. Domain contains no ambient randomness or clock.
- **Evidence:** with seed 42 and stream 54 the first six outputs are `0xa15c02b7 0x7b47f409 0xba1d3330 0x83d2f293 0xbfa4784b 0xcbed606e`, the round-one line published at [pcg-random.org](https://www.pcg-random.org/using-pcg-c-basic.html). A throwaway C# probe reproduced them on 2026-10-06.

### Streamer name

`StreamerName.TryNormalize(string input, out string normalized)` applies the documented rules: Unicode NFC, trimmed outer whitespace, 2 to 32 displayed characters counted as text elements, only letters, decimal digits, combining marks after a base character, hyphens, underscores, and single spaces between other characters, and no control characters. This spec adds two rules: at least one letter or digit, so a name such as `--` is rejected, and at most 64 UTF-16 code units, which bounds names built from long combining sequences. A stored name must already be in normalized form.

### Game parameters

`GameParameters` holds the twelve typed columns from the event system: `RunLengthWeeks`, `MaxEventsPerWeek`, `StartingMoneyPln`, `StartingViewers`, `StartingDrama`, `BankruptcyThresholdPln`, `SubscriptionViewersPerPln`, `ScoreAudienceReferenceViewers`, `ScoreProfitReferencePln`, `ScorePointsPerComponent`, `CalmDramaMax`, and `MiddleDramaMax`. Money values are `long`; the others are `int`. `GameParametersValidator` applies the published validation: run length 1 to 260, positive event cap, non-negative starting viewers, starting drama 0 to 100, a negative bankruptcy threshold below starting money, positive subscription divisor, score references, and component points, and `0 <= CalmDramaMax < MiddleDramaMax < 100`. The first published values stay content for S2's seed, not code constants.

### Catalogue definitions

- `WeeklyActionDefinition(string ActionId, long GuaranteedCostPln, IReadOnlyList<WeeklyActionOutcome> Outcomes)`.
- `WeeklyActionOutcome(string OutcomeId, int ChanceBps, int ViewersDelta, int DramaDelta, IReadOnlyList<CashFlowAmount> CashFlows)`.
- `CashFlowAmount(CashFlowCategory Category, long AmountPln)`: sponsor or donation income, or an expense.
- `TerminalReasonDefinition(string ReasonCode, TerminalClassification Classification)` with `TerminalClassification` `Success`, `Neutral`, `Defeat`.

These are shapes only. Outcome-table validation, such as chances summing to 10,000, belongs with the engine and publication checks in S1 and S2.

### Cash ledger

- `CashFlowCategory`: `Sponsors`, `Donations`, `Subscriptions`, `Expenses`.
- `CashFlowSource`: `WeeklyActionCost`, `WeeklyActionOutcome`, `Subscription`, `EncounterCost`, `ResponseCost`, `ResponseOutcome`.
- `CashFlowEntry(int Week, int Step, CashFlowSource Source, int Ordinal, CashFlowCategory Category, long AmountPln)` with a computed `SignedAmountPln` that is negative for expenses.
- `Step` 0 is the weekly action step, which also carries the subscription settlement. Step `k` from 1 is the week's `k`-th selected event.
- `Ordinal` separates several entries from one outcome; it is 0 for costs and the subscription.
- The key `(Week, Step, Source, Ordinal)` is unique, so a retried step cannot pay the same entry twice. The IDs a report needs come from the week record that `(Week, Step)` points to.

### Run state

`RunState(int RulesVersion, int CatalogVersion, Guid RunId, string StreamerName, int Week, RunStatus Status, long MoneyPln, int Viewers, int Drama, RngState Rng, IReadOnlyList<CashFlowEntry> Ledger, IReadOnlyList<WeekRecord> History, WeekInProgress? CurrentWeek, IReadOnlyList<NarrativeFlag> Flags, RunEnding? Ending)`.

Its parts:

- `RunStatus`: `Active`, `PendingWeek`, `Completed`, `Bankrupt`, `SpecialEnding`.
- `WeekRecord(int Week, ActionResolution Action, IReadOnlyList<EncounterRoll> EncounterRolls, IReadOnlyList<EventResolution> Events)`: one finished week.
- `WeekInProgress(int Week, ActionResolution Action, IReadOnlyList<EncounterRoll> EncounterRolls, IReadOnlyList<EventResolution> ResolvedEvents, PendingEvent Pending)`: the current week while an event waits for a response.
- `ActionResolution(string ActionId, int Roll, string OutcomeId, int ViewersDelta, int DramaDelta)`.
- `EncounterRoll(string EventId, int Roll, bool Passed)`: at most one per event per week.
- `PendingEvent(string EventId, int Index)`.
- `EventResolution(string EventId, int Index, ResponseResolution? Response)`. `Response` is null only for an event whose encounter cost bankrupted the run.
- `ResponseResolution(string OptionId, int Roll, string OutcomeId, int ViewersDelta, int DramaDelta, IReadOnlyList<FlagChange> FlagChanges, string? TerminalReasonCode)`.
- `FlagChange(string FlagId, FlagOperation Operation)` with `FlagOperation` `Set`, `Clear`; `NarrativeFlag(string FlagId, int SetWeek)`.
- `RunEnding(RunEndingKind Kind, int Week, string? ReasonCode)` with `RunEndingKind` `Completed`, `Bankrupt`, `Special`.

Deltas are the changes actually applied after viewers are clamped at zero and drama to 0 to 100. Week semantics:

| Status | `Week` | `History` | `CurrentWeek` | `Ending` |
| --- | --- | --- | --- | --- |
| `Active` | Next week to play | Weeks 1 to `Week - 1` | Null | Null |
| `PendingWeek` | Week in progress | Weeks 1 to `Week - 1` | Present | Null |
| `Completed`, `Bankrupt`, `SpecialEnding` | Final week | Weeks 1 to `Week` | Null | Present |

The repeat-policy view of past encounters comes from the events in `History` and `CurrentWeek`, so no separate encounter list can disagree with them.

### Validation

`RunStateValidator.Validate(RunState state, GameParameters parameters)` returns `RunStateValidation` with `IsValid`, `Errors`, and, when valid, `ValidatedRunState Value`. `ValidatedRunState` has no public constructor and exposes `State` and `Parameters`. The validator first validates the parameters and stops with `invalid_game_parameters` if they fail. Each `RunStateError` carries a stable `Code` from `RunStateErrorCodes` and a `Path` such as `ledger[3]` for diagnostics; the validator reports every error it finds.

| Code | Rule |
| --- | --- |
| `unsupported_rules_version` | `RulesVersion` equals `RulesVersion.Current` |
| `invalid_catalog_version` | `CatalogVersion` is at least 1 |
| `invalid_run_id` | `RunId` is not empty |
| `invalid_streamer_name` | The name is valid and already normalized |
| `unsupported_rng_algorithm` | `Rng.Algorithm` is `pcg32` |
| `week_out_of_range` | `Week` is 1 to `RunLengthWeeks` |
| `viewers_negative` | `Viewers` is at least 0 |
| `drama_out_of_range` | `Drama` is 0 to 100 |
| `invalid_stable_id` | Every action, outcome, event, option, flag, and reason ID is a stable ID |
| `status_inconsistent` | `CurrentWeek` and `Ending` are present exactly as the week semantics table says, and `Ending.Kind` matches the status |
| `ending_inconsistent` | `Ending.Week` equals `Week`; `Completed` ends at `RunLengthWeeks`; only `Special` carries a reason code, which equals the terminal reason of the final week's last response |
| `money_at_or_below_bankruptcy_threshold` | Only `Bankrupt` may have money at or below the threshold |
| `bankrupt_money_above_threshold` | `Bankrupt` has money at or below the threshold |
| `history_inconsistent` | History holds weeks 1 to N in order, as the week semantics table says; `CurrentWeek.Week` equals `Week` |
| `event_cap_exceeded` | A week holds at most `MaxEventsPerWeek` events, counting the pending one |
| `event_index_inconsistent` | Event indexes in a week run 1, 2, 3 without gaps; the pending index follows the resolved ones |
| `event_selected_twice` | An event appears at most once per week |
| `encounter_rolled_twice` | An event has at most one encounter roll per week |
| `selected_event_without_passing_roll` | Every selected event has a passing encounter roll in its week |
| `unanswered_event` | Every event has a response, except the last event of the final week of a `Bankrupt` run |
| `terminal_reason_not_applied` | No response carries a terminal reason except the last response of the final week of a `Bankrupt` or `SpecialEnding` run; a `Completed` run has none, because a special ending in the final week ends the run before completion |
| `roll_out_of_range` | Every action, encounter, and response roll is 0 to 9,999 |
| `ledger_week_out_of_range` | Every entry's week has a week record in `History` or `CurrentWeek` |
| `ledger_step_invalid` | Step 0 holds only weekly-action and subscription sources; step `k` holds only event sources and points to an existing event `k`; response sources point to an event with a response |
| `ledger_category_mismatch` | Costs are expenses, the subscription source is a subscription, and outcome sources are sponsors, donations, or expenses |
| `ledger_amount_invalid` | Subscription entries are at least 0 PLN; all other entries are above 0 PLN; ordinals are 0 for costs and subscriptions and at least 0 otherwise |
| `ledger_duplicate_entry` | `(Week, Step, Source, Ordinal)` is unique |
| `subscription_entry_count` | Each week in `History`, and the week in `CurrentWeek`, has exactly one subscription entry |
| `money_not_reconciled` | `MoneyPln` equals `StartingMoneyPln` plus every signed entry |
| `viewers_not_reconciled` | `Viewers` equals `StartingViewers` plus every action and response viewer delta |
| `drama_not_reconciled` | `Drama` equals `StartingDrama` plus every action and response drama delta |
| `flag_invalid` | Flag IDs are unique and each `SetWeek` is 1 to `Week` |

The validator deliberately checks totals and structure, not a full replay: it does not recompute clamping step by step or prove that every historical checkpoint obeyed the bankruptcy rule. The player owns the save, so the goal is a consistent state the engine can continue, not anti-cheat protection.

### Wire contracts

DTO records in `Contracts` mirror the domain records one to one, with the suffix `Dto`: `RunStateDto`, `RngStateDto`, `CashFlowEntryDto`, `WeekRecordDto`, `WeekInProgressDto`, `ActionResolutionDto`, `EncounterRollDto`, `PendingEventDto`, `EventResolutionDto`, `ResponseResolutionDto`, `FlagChangeDto`, `NarrativeFlagDto`, `RunEndingDto`, and the enums `RunStatusDto`, `CashFlowCategoryDto`, `CashFlowSourceDto`, `FlagOperationDto`, `RunEndingKindDto`. `RunStateDto` adds `SchemaVersion` as its first property.

Requests and responses:

- `SetupOptionsResponse(int CatalogVersion, int RunLengthWeeks, long StartingMoneyPln, int StartingViewers, int StartingDrama, long BankruptcyThresholdPln, string SuggestedStreamerName)`.
- `StreamerNameSuggestionResponse(int CatalogVersion, string StreamerName)`.
- `StartRunRequest(int CatalogVersion, string StreamerName)`.
- `PlanWeekRequest(RunStateDto State, string ActionId)`.
- `ChooseEventOptionRequest(RunStateDto State, string OptionId)`.
- `StartRun`, `PlanWeek`, and `ChooseEventOption` all return the resulting `RunStateDto`.

Supporting types:

- `SupportedLocales`: `Polish = "pl-PL"`, `English = "en"`, `Default = Polish`, and `IsSupported(string)`. Locale stays a query parameter and never enters the state.
- `ProblemCodes`: `invalid_request`, `invalid_state`, `invalid_choice`, `invalid_streamer_name`, `incompatible_save`, `catalog_version_unavailable`, `run_finished`. S4 and S5 map them to Problem Details.

JSON conventions, applied by one source-generated `ContractsJsonContext`:

- camelCase property names.
- Every constructor property is required; nullable properties are written and must be present as `null`.
- Unknown properties are rejected.
- Enums travel as fixed camelCase names set per member with `JsonStringEnumMemberName`; numbers and unknown names are rejected through a strict string enum converter.
- `RngStateDto` writes `seed`, `stream`, and `state` as 16-digit lowercase hexadecimal strings, because JSON numbers above 2^53 lose precision in JavaScript tools.
- `RunId` is a standard GUID string; money values are JSON numbers.

A throwaway probe on 2026-10-06 confirmed that the .NET 10 source generator with these options rejects a missing property, a null in a non-nullable property, an unknown property, an unknown or numeric enum value, and malformed hexadecimal.

A pending week, abbreviated:

```json
{
  "schemaVersion": 1, "rulesVersion": 1, "catalogVersion": 1,
  "runId": "3f2b8c4e-6a1d-4c2e-9b7a-5d0e8f1a2b3c", "streamerName": "NeonBorsuk",
  "week": 3, "status": "pendingWeek", "moneyPln": 1430, "viewers": 51, "drama": 47,
  "rng": { "algorithm": "pcg32", "seed": "000000000000002a", "stream": "0000000000000036", "state": "5f0e2b1a9c3d4e7f" },
  "ledger": [ { "week": 3, "step": 0, "source": "subscription", "ordinal": 0, "category": "subscriptions", "amountPln": 5 } ],
  "history": [ ],
  "currentWeek": {
    "week": 3,
    "action": { "actionId": "regular_stream", "roll": 1783, "outcomeId": "steady", "viewersDelta": 8, "dramaDelta": 0 },
    "encounterRolls": [ { "eventId": "meme_misread", "roll": 412, "passed": true } ],
    "resolvedEvents": [ ],
    "pending": { "eventId": "meme_misread", "index": 1 }
  },
  "flags": [ ],
  "ending": null
}
```

The example shortens `history` and `ledger`; a real week-3 state carries both earlier weeks and their entries.

## Testing

All new code is written test-first.

| Area | Tests |
| --- | --- |
| `Pcg32` | Seed 42 and stream 54 give the six published outputs; `NextRoll` stays within 0 to 9,999; a state whose next output falls below 7,296 consumes a second output; the same state always yields the same roll sequence; `ToState` resumes exactly |
| `StreamerName` | Accepts `NeonBorsuk`, `Cichy Kret`, `Zażółć_gęślą-1`, and two-letter names; normalizes NFD to NFC and trims; rejects one character, 33 characters, double or edge spaces, control characters, emoji, symbols, names without a letter or digit, and over-long combining sequences |
| `GameParametersValidator` | The first published values pass; each published rule fails on its boundary |
| `RunStateValidator` | Valid fixtures for each of the five statuses; for every error code, the smallest change to a valid fixture produces that code; several errors are reported together; a valid result exposes a `ValidatedRunState` |
| Wire format | Pending and terminal fixtures round-trip without change; exact JSON names for every enum value; hexadecimal generator fields; rejection of a missing property, a null in a non-nullable property, an unknown property, a numeric or unknown enum, and bad hexadecimal |

## Documentation updates

- Technical design: a "Run state and wire format" section that summarizes the model, the validated-state token, PCG32, the JSON conventions, and the problem codes, replacing the free-form field list.
- Balance: the subscription entry may be 0 PLN, and rolls use the PCG32 bounded draw.
- Streamer name generation: the two added rules, now implemented.
- Testing strategy: `Domain.Tests` exists; wire format tests live in `Server.IntegrationTests`.
- Decisions: new rows for the random generator and the run-state wire format; D-34's name validation moves from proposed to implemented.
- Library guide: source-generated `System.Text.Json` for contracts.
- Delivery plan: F2 progress.

## Risks

- **Strict JSON blocks forward compatibility.** That is intended: any shape change raises `SaveSchema.CurrentVersion`, and M3 decides whether old saves migrate.
- **The validator grows with the engine.** S1 and S3 may add rules. Each rule stays one row in the table above and one test.
- **Deferred event definitions** could pressure the ledger shape. The six source kinds already cover encounter costs, response costs, and response outcomes, so S3 should only add definitions, not change saved entries.
