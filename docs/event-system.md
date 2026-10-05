# SQLite event system

## Purpose and data boundary

SQLite is an authoring and runtime catalogue of **possible events shared by every run**. It holds the event's Polish copy, when it may occur, its dice chances, the options offered to the player, and the possible outcomes and rewards. It does **not** hold a player, run, save slot, selected option, or encounter history. A player's encountered event IDs and results are stored only in that player's browser save.

The domain engine receives immutable event definitions from an `IEventCatalog` port. `Infrastructure` loads and validates those definitions from SQLite. The domain engine evaluates conditions and applies effects; it never queries SQLite or evaluates database-supplied scripts. This keeps content editable without moving business rules into the database.

## Proposed relational schema

| Table | Key fields | Purpose |
| --- | --- | --- |
| `CatalogVersion` | `VersionId`, `Status`, `PublishedUtc`, `ContentHash` | Distinguishes editable drafts from immutable published catalogues |
| `EventDefinition` | `(VersionId, EventId)`, `OccurrenceChanceBps`, `CooldownWeeks`, `Enabled`, `TitlePl`, `PromptPl` | One possible event and its per-eligible-week dice chance |
| `EventCondition` | `(VersionId, EventId, Ordinal)`, `Field`, `Operator`, `Value` | Typed conditions combined with AND in the MVP |
| `EventOption` | `(VersionId, EventId, OptionId)`, `LabelPl`, `PreviewPl` | Player choices, in stable display order |
| `EventOutcome` | `(VersionId, EventId, OptionId, OutcomeId)`, `ChanceBps`, `ResultPl` | One possible result after choosing an option |
| `EventEffect` | `(VersionId, EventId, OptionId, OutcomeId, Ordinal)`, `Metric`, `Delta` | Additive metric, follower, or PLN effect of that result |

`Bps` means basis points: `10,000 = 100%`, `500 = 5%`. The occurrence chance is evaluated when the event is eligible and not on cooldown. Outcome chances for each option must sum to 10,000; an ordinary deterministic option has one outcome at 10,000. Exact initial values live in [event catalogue](event-catalogue.md).

Allowed condition fields in the first release: `streamedThisWeek`, `formatId`, `followers`, `trust`, `reputation`, `energy`, `equipment`, and `cashPln`. Allowed operators: `equals`, `notEquals`, `lessThan`, `lessOrEqual`, `greaterThan`, and `greaterOrEqual` where appropriate. The importer rejects unsupported combinations. Conditions use the post-baseline, pre-event state. Allowed effect targets are followers, cash, energy, trust, and reputation. The engine clamps and caps effects according to [balance](balance.md). Never store C# type names, SQL fragments, reflection paths, or arbitrary formulas as executable event content.

## Dice sequence

1. Resolve the ordinary weekly baseline and format effects. Check milestones; a baseline milestone notification takes the week's event slot.
2. Otherwise load the run's pinned published catalogue version, filter enabled events by typed conditions and cooldown, then sort by stable `EventId`.
3. For each eligible event, consume one seeded random roll from 0 through 9,999. The event passes if the roll is below `OccurrenceChanceBps`.
4. If several events pass, consume one more seeded roll to select one uniformly among those that passed. Show at most one event. If none pass, complete an ordinary week.
5. Persist the pending event, selected event ID, and new RNG state in the browser save before requesting a choice. For the chosen option, consume a seeded outcome roll and select one outcome by its `ChanceBps` interval. Apply its effects once, record the outcome ID and deltas, then save the completed week.

An event's displayed `OccurrenceChanceBps` is its chance to pass its own eligible-week roll; the chance that it is finally shown can be lower if several events pass. Do not describe it to players as an unconditional chance. The event's option preview should distinguish guaranteed effects from risky outcomes.

## Catalogue versioning and updates

- `StartRun` pins the latest published `VersionId` in the browser save. Every later week loads that exact version. Database row order must never affect seeded selection.
- A published version is immutable. Editing creates a new draft version; existing runs continue using the old one. Keep old published versions while compatible browser saves may still exist.
- For the MVP, maintain human-reviewed event definitions in a source-controlled import file and provide a development/import command that validates and writes a draft to SQLite. Publish in one transaction after all checks pass. This is a proposal; an authenticated admin editor can be added if content authors need one in the first release.
- The import/publish check verifies unique stable IDs, valid Polish text, at least two selectable options, valid condition/effect kinds, effect limits, cooldown bounds, occurrence chance from 0 to 10,000, and per-option outcome chances summing to 10,000.
- Keep EF Core migrations separate from content updates. Schema migrations change table shape; draft/publish changes game content. Back up the SQLite file before deploying either.
- If a saved run requests a catalogue version that the server no longer has, return an explicit `catalog_version_unavailable` error and leave the browser save intact. Do not silently switch that run to a newer catalogue.

## Content management boundaries

The public player API can read and use published events but cannot create, edit, or publish them. The initial import command is a trusted developer operation, not an unauthenticated HTTP endpoint. If an admin UI is later introduced, it needs authentication, audit information, preview, and publish permissions. The SQLite file must remain server-side and must not be copied into Blazor's public `wwwroot`.

## Example of one option with a reward roll

The `clip_spread` event can have a promotional option with two outcomes: a 70% result of `+60 followers, -2 trust`, and a 30% result of `+10 followers, -2 trust`. Its cautious option can have one 100% result of `+25 followers, +2 trust`. The event itself is encountered only if eligible and its separate occurrence roll passes. This distinction between **encounter chance** and **outcome chance** applies to every event.
