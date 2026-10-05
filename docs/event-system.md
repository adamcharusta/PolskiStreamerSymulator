# SQL Server game catalogue

## Purpose and data boundary

SQL Server is an authoring and runtime catalogue of **shared game data**: possible events, weekly-action definitions, typed game parameters, and parts for suggested streamer names. It holds player-facing option copy, chances and effects; starting values, run length, bankruptcy and balance constants; and the name vocabulary. It does **not** hold a player, run, save slot, chosen name, selected option, or encounter history. A player's identity, choices, rolls, and results are stored only in that player's browser save. The content target is at least 200 distinct events; the first documentation pass contains [20 draft examples](sample-events.md). The weekly action menu remains open for authoring.

The domain engine receives immutable event and weekly-action definitions plus typed `GameParameters` through Application ports. `Infrastructure` loads and validates them from the same published SQL Server catalogue version. The domain engine applies the content to its coded rules; it never queries SQL Server or evaluates database-supplied scripts. The database supplies values and weighted outcome tables, while formulas and invariants remain in `Domain`. Name suggestions are a setup feature and do not consume the gameplay random stream.

## Proposed relational schema

| Table | Key fields | Purpose |
| --- | --- | --- |
| `CatalogVersion` | `VersionId`, `Status`, `PublishedUtc`, `ContentHash` | Distinguishes editable drafts from immutable published catalogues |
| `GameParameters` | `VersionId`, typed numeric columns described below | Exactly one validated parameter row per catalogue version |
| `StreamerNamePart` | `(VersionId, Kind, PartId)`, `Text`, `Enabled` | Approved first or second name parts for suggestions; no player-chosen names |
| `WeeklyActionDefinition` | `(VersionId, ActionId)`, `LabelPl`, `PreviewPl`, `GuaranteedCostPln`, `Enabled`, `SortOrder` | One player-selectable action per week; the final menu is still to be authored |
| `WeeklyActionOutcome` | `(VersionId, ActionId, OutcomeId)`, `ChanceBps`, `ResultPl` | Weighted consequence of a weekly action |
| `WeeklyActionEffect` | `(VersionId, ActionId, OutcomeId, Ordinal)`, `Metric`, `Delta` | Additive viewer or drama effect |
| `WeeklyActionCashFlow` | `(VersionId, ActionId, OutcomeId, Ordinal)`, `Category`, `AmountPln` | Sponsor or donation income, or an expense; subscription remains the separate weekly formula |
| `EventDefinition` | `(VersionId, EventId)`, `OccurrenceChanceBps`, `EncounterCostPln`, `CooldownWeeks`, `Enabled`, `TitlePl`, `PromptPl` | One possible event; an optional unavoidable cost is charged only if this event is selected |
| `EventCondition` | `(VersionId, EventId, Ordinal)`, `Field`, `Operator`, `Value` | Typed money, viewer, drama, week, and narrative-flag conditions combined with AND in the MVP |
| `EventOption` | `(VersionId, EventId, OptionId)`, `LabelPl`, `PreviewPl`, `GuaranteedCostPln` | Player choices, in stable display order; an optional cost becomes one expense entry before the roll |
| `EventOutcome` | `(VersionId, EventId, OptionId, OutcomeId)`, `ChanceBps`, `ResultPl` | One possible result after choosing an option |
| `EventEffect` | `(VersionId, EventId, OptionId, OutcomeId, Ordinal)`, `Metric`, `Delta` | Additive viewer or drama effect of that result |
| `EventCashFlow` | `(VersionId, EventId, OptionId, OutcomeId, Ordinal)`, `Category`, `AmountPln` | Sponsor or donation income, or an expense; amount is a non-negative magnitude with direction defined by category |

`Bps` means basis points: `10,000 = 100%`, `500 = 5%`. The occurrence chance is evaluated when the event is eligible and not on cooldown. Outcome chances for each event response **and each weekly action** must sum to 10,000. The creator wants every player choice to have percentage-based consequences, so authoring should provide at least two meaningfully distinct outcomes per choice; a guaranteed cost is shown separately. Exact event odds belong in [event catalogue](event-catalogue.md); the weekly menu remains to be authored in [balance](balance.md).

### Versioned game parameters

The first published `GameParameters` row uses these values. They are **initial content values**, not constants compiled into gameplay code. Confirmed game decisions remain the intended defaults; changing them later creates a new published version and affects new runs only.

| Typed column | Initial value | Domain use |
| --- | ---: | --- |
| `RunLengthWeeks` | 52 | Final week; no hard-coded week 52 check |
| `StartingMoneyPln` | 1,500 | Opening balance and base subtracted when calculating net profit |
| `StartingViewers` | 20 | Opening regular audience |
| `StartingDrama` | 50 | Opening public controversy level |
| `BankruptcyThresholdPln` | -1,000 | Inclusive defeat limit |
| `SubscriptionViewersPerPln` | 10 | Divisor in the proposed weekly subscription formula |
| `ScoreAudienceReferenceViewers` | 1,000 | Audience scale in the proposed final-score formula |
| `ScoreProfitReferencePln` | 5,000 | Net-profit scale in the proposed final-score formula |
| `ScorePointsPerComponent` | 500 | Equal point multiplier for audience and profit |
| `CalmDramaMax` | 33 | Last value in the calm band |
| `MiddleDramaMax` | 66 | Last value in the middle band; high begins above it |

Import validation requires exactly one row per version; integer types; `RunLengthWeeks` in a proposed operational range of 1–260; non-negative starting viewers; starting drama within 0–100; a negative bankruptcy threshold below starting money; positive subscription divisor and score references; positive equal component points; and `0 <= CalmDramaMax < MiddleDramaMax < 100`. Reject missing, duplicate, unknown, or invalid fields, and reject event week conditions that are impossible under the configured run length. Use typed columns rather than an arbitrary key/value expression or executable formula. Changing an existing value creates a new draft and published catalogue version without changing gameplay code. Adding a new kind of parameter requires an EF migration, validation, and Domain support. Publishing freezes parameters, weekly actions, events, and name parts together and includes all of them in `ContentHash`. A saved run pins its catalogue version and therefore its exact parameter row. The database stores no current player balance or defeat status.

The `StreamerNamePart` content contract and suggestion behavior are in [streamer name generation](streamer-name-generation.md). Publish at least two enabled first parts and two enabled second parts. The server selects one of each from the pinned setup catalogue version, concatenates them, and returns a suggestion. The chosen name stays in browser state, never in this table.

Allowed condition fields in the first release: `week`, `weeklyActionId`, `viewers`, `moneyPln`, `drama`, `dramaBand`, and a controlled set of narrative flags. Allowed operators: `equals`, `notEquals`, `lessThan`, `lessOrEqual`, `greaterThan`, and `greaterOrEqual` where appropriate. A band list can express eligibility for multiple drama ranges using the pinned parameter boundaries. The importer rejects unsupported combinations. Conditions use the state after the one weekly action **and weekly subscription settlement**, before event effects. `EventEffect` targets are only viewers and drama; all changes to money use typed `EventCashFlow` entries. The event catalogue may author sponsor/donation income and expenses. The regular subscription payout is a coded weekly rule using the pinned divisor and cannot be emitted a second time by an event. The engine clamps viewers and drama according to [balance](balance.md). Never store C# type names, SQL fragments, reflection paths, or arbitrary formulas as executable event content.

## Dice sequence

1. Resolve the selected weekly action with one weighted outcome roll, including any sponsor/donation/expense entries. Settle subscription income once using the resulting viewers, the pinned `SubscriptionViewersPerPln`, and the versioned Domain formula. Reconcile the action and subscription entries, then compare the balance with the pinned bankruptcy threshold. If it is at or below the limit, return a defeat state and recap for browser storage, then stop. Otherwise retain the resulting state and RNG state before evaluating events. If a one-time milestone uses the event slot, record it and complete the week.
2. Otherwise load the run's pinned published catalogue version, filter enabled events by money, viewers, drama band, week, flags, and cooldown, then sort by stable `EventId`.
3. For each eligible event, consume one seeded random roll from 0 through 9,999. The event passes if the roll is below `OccurrenceChanceBps`.
4. If several events pass, consume one more seeded roll to select one uniformly among those that passed. Show at most one event. If none pass, complete an ordinary week. Charge `EncounterCostPln` **once for the selected event only** and record an expense entry with that event ID; this may create debt. If the resulting balance is at or below the pinned threshold, return a defeat state and recap with that event and cost, and do not offer responses.
5. Otherwise persist the pending event, selected event ID, paid encounter cost, completed weekly-action roll and subscription entry, and new RNG state in the browser save before requesting a response. Determine response affordability from this post-encounter-cost state. For the chosen option, record any `GuaranteedCostPln` as a distinct expense, consume a seeded outcome roll, and select one outcome by its `ChanceBps` interval. Apply its viewer/drama effects and cash-flow entries as one response step, record the roll, outcome ID, and deltas, then check the threshold. Save either the completed week or the defeat recap; do not allow another choice after defeat.

An event's displayed `OccurrenceChanceBps` is its chance to pass its own eligible-week roll; the chance that it is finally shown can be lower if several events pass. Do not describe it to players as an unconditional chance. The event's option preview should distinguish guaranteed effects from risky outcomes.

## Catalogue versioning and updates

- Setup reads a published `VersionId` and its parameters. `StartRun` pins that version in the browser save, even if a new catalogue is published while setup is open. Every later week loads that exact version. Database row order must never affect seeded event selection.
- A published version is immutable. Editing creates a new draft version; existing runs continue using the old one. Keep old published versions while compatible browser saves may still exist.
- For the MVP, maintain human-reviewed weekly actions and events, one typed parameter set, and name parts in source-controlled import content; a development/import command validates and writes a draft to SQL Server. Publish all of it in one transaction after checks pass. This is a proposal; an authenticated admin editor can be added if content authors need one in the first release.
- The import/publish check verifies one valid `GameParameters` row, valid name parts in both groups, at least one enabled weekly action with stable IDs and valid weighted outcomes, unique stable event IDs, valid Polish text, at least two authored responses per event including a free fallback, at least two meaningfully distinct outcomes per choice, valid money/viewer/drama conditions and effects, sponsor/donation/expense entries, non-negative guaranteed/encounter/outcome cost magnitudes, effect limits, cooldown bounds, occurrence chance from 0 to 10,000, and per-choice outcome chances summing to 10,000.
- Keep EF Core migrations separate from content updates. Schema migrations change table shape; draft/publish changes game content. Take a SQL Server database backup before deploying either.
- If a saved run requests a catalogue version that the server no longer has, return an explicit `catalog_version_unavailable` error and leave the browser save intact. Do not silently switch that run to a newer catalogue.

## Content management boundaries

The public player API can read and use published events, read the public setup parameters, and request a name suggestion. It cannot create, edit, or publish catalogue content. The initial import command is a trusted developer operation, not an unauthenticated HTTP endpoint. If an admin UI is later introduced, it needs authentication, audit information, preview, and publish permissions. SQL Server is reachable only inside the cluster; connection strings and backups must not be copied into Blazor's public `wwwroot`.

## Example of one option with a reward roll

A `clip_spread` response could have a 70% result of `+60 viewers, +4 drama` and a 30% result of `+10 viewers, +1 drama`. A cautious response could have an 80% result of `+20 viewers, -1 drama` and a 20% result of no change. These are example odds for the data model, not approved balance values. The event itself is encountered only if eligible and its separate encounter roll passes. This distinction between **encounter chance** and **outcome chance** applies to every event.
