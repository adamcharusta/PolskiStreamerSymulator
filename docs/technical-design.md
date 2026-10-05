# Technical design: .NET, CQRS, and event content

## Observed solution and approved direction

The repository contains a .NET 10 solution under `PolskiStreamerSymulatorApp/`. `global.json` requests SDK `10.0.201` with `latestFeature` roll-forward; the installed `10.0.401` SDK built the original skeleton successfully on 2026-10-04. Current projects are `Domain`, `Application`, `Infrastructure`, and standalone Blazor WebAssembly `BlazorApp`. Central package management and warnings-as-errors are enabled.

The creator confirmed a CQRS architecture and an **ASP.NET Core server**. The server hosts the WebAssembly client, runs Wolverine handlers, and reads a SQL Server game catalogue containing events, weekly-action outcome data, versioned numeric parameters, and streamer-name parts. **Career state and saves stay in the browser; no player or run records go into SQL Server.** The server and contract projects below are target architecture, not existing code.

## Target project graph

```text
Domain                 (no project references)
Application  ----------> Domain
Infrastructure -------> Application, Domain
Contracts              (HTTP DTOs; no project references)
Server ---------------> Application, Infrastructure, Contracts, BlazorApp
BlazorApp ------------> Contracts
```

`Contracts` and `Server` are proposed new projects. `Server` is the ASP.NET Core composition root and API host; `BlazorApp` remains a WebAssembly view served from the same origin. The current client-to-Infrastructure project reference must be removed when this graph is implemented. [Microsoft documents](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/?view=aspnetcore-10.0) this hosted WebAssembly model.

| Project | Owns | Does not own |
| --- | --- | --- |
| `Domain` | Game entities/value objects, invariants, pure weekly simulation, seeded RNG contract, event eligibility and effect rules | EF Core, HTTP, Blazor, Wolverine, Mapster |
| `Application` | CQRS commands/queries and handlers, game use cases, event/parameter/name catalogue ports, an analytics sink port, command validators | EF entities, UI, browser storage |
| `Infrastructure` | EF Core SQL Server catalogue and aggregate analytics contexts, migrations, repositories, and content import/publish tooling | Player saves, gameplay formulas |
| `Contracts` | Versioned API request/response records, save DTO, stable error codes | Domain behavior and database entities |
| `Server` | Host and DI composition, Wolverine configuration, HTTP endpoints, structured logging, operational metrics, size limits, transport error mapping, static client hosting | Gameplay formulas or player persistence |
| `BlazorApp` | Polish components, screen state, API client, browser save adapter, app-open/client-error reporting, accessibility | SQL Server access or trusted gameplay rules |

`Infrastructure` registers its adapters; `Server` composes the full application. Refactor the current empty registration extensions in `Application` and `Infrastructure` to extend `IServiceCollection` rather than `WebAssemblyHostBuilder`, then remove their `Microsoft.AspNetCore.Components.WebAssembly` references. `BlazorApp` should reference only `Contracts` among the project libraries.

## CQRS flow and local career state

Commands express intent: `StartRun`, `PlanWeek`, and `ChooseEventOption`. `GetSetupOptions` and `GetStreamerNameSuggestion` read the published catalogue before a run starts; the latter is described in [streamer name generation](streamer-name-generation.md). Dashboard and history are projections of the browser's saved run state; do not send a server query for data already in that state. Use feature folders in `Application`, with each command, handler, validator, and result together.

1. `GetSetupOptions` returns a published catalogue version, its public starting values and run length, and a suggested streamer name. The player can regenerate or type a name. `StartRun` receives that version and the chosen name, validates both, loads its typed parameters, and returns a new `RunStateDto` with the configured starting state, seed, RNG state, save schema version, pinned catalogue version, and code **rules version**. This code version protects algorithm semantics; the catalogue version protects action/event odds and numeric values. If a new version was published while setup was open, retain the selected published version rather than silently changing the player's displayed setup.
2. `PlanWeek` receives the current `RunStateDto` and one selected weekly action. The server validates size/schema/IDs and the bounds of money, viewers, and drama, reconstructs a domain state, loads the pinned weekly action, events, and parameters from SQL Server, invokes the pure simulation, and returns a new state. The weekly action is resolved through one seeded percentage roll against its pinned outcome table; its categorized cash flows are recorded, then subscription revenue uses the pinned divisor once before event eligibility. If the resulting balance is at or below the pinned threshold, return a terminal defeat recap. Otherwise, if an event is selected, charge its automatic encounter cost once; return a terminal defeat if that reaches the threshold, or `pendingWeek` if it does not. Since earlier turns are not stored server-side, the server cannot prove that client-supplied historical values are authentic.
3. Blazor writes the pending or terminal state, including any already paid encounter expense, in browser storage before displaying the next screen. `ChooseEventOption` accepts only a pending state and response ID, checks affordability against the post-encounter balance, charges any response cost once, consumes one seeded outcome roll, applies it once, and returns the completed report or bankruptcy defeat. Blazor saves it before advancing the UI.

The server does **not** retain run state between requests. The same input state, catalogue version, and choice must produce the same result, which makes a retry safe. Name-suggestion randomness is independent and cannot advance this gameplay random stream. The UI must disable duplicate submission and reject a response for an older local week. Reject an `active` or `pendingWeek` state whose money is already at or below its pinned bankruptcy threshold, as well as any command attempting to advance `completed` or `bankrupt` state. Because the player owns the local save, they can alter it; this is acceptable for a single-player game without rankings. The server still validates untrusted input, bounds request size, and never logs the state body or player-entered names. If server-authoritative saves, multiplayer, or leaderboards are added later, this protocol must be redesigned.

The `RunStateDto` needs `schemaVersion`, `rulesVersion`, `catalogVersion`, `runId`, seed/RNG algorithm and state, chosen streamer name and player setup, week/status (`active`, `pendingWeek`, `completed`, or `bankrupt`), **money, viewers, drama**, pending week, and history. Include milestone IDs only if milestones remain. The pinned catalogue version determines run length, starting values, bankruptcy threshold, drama bands, subscription divisor, and score reference values; do not trust copies of these parameters supplied by the browser. The domain model is reconstructed through validation methods, not blindly mapped from arbitrary JSON. Store the weekly action ID, event and response IDs, rolls, outcome IDs, viewer/drama deltas, and categorized cash-flow entries in history so later text edits do not rewrite an old report. Expose the four weekly cash subtotals and opening/closing money through report DTOs without inventing new persistent game statistics. At either ending, `Domain` calculates the single score and its two rounded components from final viewers and the full-run cash ledger using the pinned parameter values and code formula version; `Application` returns them with the ending reason in the recap response, and Blazor only displays them. The score is derived, not an additional mutable run statistic.

## Wolverine, Mapster, and FluentValidation

- **WolverineFX:** Configure it in `Server`, explicitly discover handlers in `Application`, and use `IMessageBus.InvokeAsync` for request/response. Start in [mediator-only mode](https://wolverinefx.net/tutorials/mediator.html); no queue, outbox, or per-player message persistence is needed. Review [assembly discovery](https://wolverinefx.net/guide/handlers/discovery) because handlers live outside the host project.
- **Mapster:** Use it for simple API DTO to Application command/result mappings at the server boundary. Never use a mapper to bypass domain validation when rebuilding `RunState`. Centralize and test nontrivial mappings. The [official project](https://github.com/MapsterMapper/Mapster) also supports generated mappings.
- **FluentValidation:** Validate command shape and cross-field input rules in `Application`; `Domain` still protects invariants. Wolverine has [FluentValidation middleware](https://wolverinefx.net/guide/handlers/fluent-validation). Choose one validator-registration path, and return stable 400 Problem Details responses for invalid API input. The old `FluentValidation.AspNetCore` automatic MVC integration [does not apply to Blazor or Minimal APIs](https://docs.fluentvalidation.net/en/latest/aspnet.html). Blazor's own form validation provides immediate UI feedback.

Add Mapster and FluentValidation when the first real mapping and validator exist. Keep package versions in `Directory.Packages.props` and check compatibility with the solution's .NET version.

## SQL Server is a versioned game catalogue

The SQL Server switch is a **proposed infrastructure choice**, not a gameplay requirement. The expected first catalogue of roughly 200 events and aggregate counters would also fit SQLite. SQL Server plus EF Core offers the creator a familiar relational tooling path and room for a future multi-author editor, but uses more memory and requires separate database operations on the 8 GB VPS. Choose SQL Server 2025 Express only after validating that the operational cost is acceptable; no existing player data needs migration because the repository has no game persistence yet.

SQL Server stores **content shared by all players**: event definitions and outcomes, weekly-action definitions and weighted outcomes, one typed `GameParameters` row per published version, and first/second streamer-name parts. Initial parameters include 52 weeks, 1,500 PLN, 20 viewers, drama 50, bankruptcy at -1,000 PLN, and the provisional balance constants in [balance](balance.md). The regular subscription and final-score **formula algorithms** remain in versioned Domain code, while their tunable numeric values and action outcome tables come from SQL Server. SQL Server stores no chosen names, player viewer counts, run history, defeat records, or save slots. EF Core models and migrations belong to `Infrastructure`. Gameplay should query a published catalogue version and evaluate typed rules in `Domain`; it must not execute SQL expressions or arbitrary code supplied by content rows. See [event system](event-system.md) for schema and publishing, and [streamer name generation](streamer-name-generation.md) for the setup feature.

An active run pins one immutable catalogue version. Editing a draft and publishing a new version affects new runs, while an old version remains available for existing local saves. The server may cache an immutable published catalogue per version and invalidate only when a new version is published. Use `Microsoft.EntityFrameworkCore.SqlServer` with two DbContexts and separate databases on one SQL Server instance: `PssCatalog` for published content and `PssAnalytics` for aggregate counters. Keep EF migrations and credentials separate by database. The SQL Server instance and its backups must persist across application deployments. [Microsoft's EF Core SQL Server provider](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/) supports this stack.

The code `rulesVersion` is separate from the data `catalogVersion`. Pure rule algorithms remain in code; when their meaning changes, preserve previous code rules for compatible saves or provide an explicit save migration. Changes to action or event outcome tables, numeric parameters, or name vocabulary create a new catalogue version and do not silently recalculate an old run.

## Browser saves and deployment

Use a small `IBrowserSaveStore` abstraction in `BlazorApp`, implemented with JS interop and browser `localStorage` for the initial 52-week configuration. Reserve one autosave and up to three named manual slots. Serialize the complete versioned state to one key per slot, validate it before writing, catch quota/storage errors, and show a failure instead of claiming success. On load, validate schema, code rules, and catalogue versions; an incompatible save remains untouched with a clear message. Add export/import later if players need transfer or backup. Browser storage is per browser profile and can be cleared by the user.

Deploy the client and API on one HTTPS origin. The server connects to SQL Server through an internal Kubernetes service using a dedicated application login, not `sa`; database credentials are supplied through Kubernetes Secrets and never reach the browser. Migrations and content publishing use a separate privileged operator path. Do not add accounts, player cookies, or server-side player records for the MVP. The selected single-VPS k3s topology, provisioning, release procedure, and current implementation gates are in [deployment](deployment.md).

## Logs and aggregate analytics

Use built-in ASP.NET Core `ILogger<T>` with structured JSON console output for information, warnings, and errors, plus built-in .NET meters for request health. Store product counters in the **separate** `PssAnalytics` database on the same SQL Server instance; its writes cannot mutate the published `PssCatalog` catalogue. Successful CQRS command results emit allowlisted facts through an `IAnalyticsSink`; a bounded Infrastructure writer aggregates them by day, catalogue version, week, and stable choice IDs. The Blazor client reports an app open and restricted client errors through first-party endpoints. No names, save bodies, raw run IDs, IP addresses, or individual career history go into analytics storage. A failed analytics write cannot fail gameplay. The definitions, privacy boundaries, retention, and operator reports are in [logging and analytics](observability-and-analytics.md).

## Technical quality requirements

- Domain simulation has no ambient clock, `Random.Shared`, database, or browser dependency. Persist the PRNG algorithm/version and state in the local save.
- Sort eligible event IDs before probability rolls so database row order cannot change a seeded run.
- Validate that every weekly and event-response outcome distribution sums to 10,000 basis points; apply exactly one outcome per choice.
- Require every money change to produce a sponsor, donation, subscription, or expense entry. Reconcile `closingMoney` against the opening balance and those entries before returning a weekly report.
- Reconcile full-run net profit against `finalMoney - pinned StartingMoneyPln` before scoring. The score uses final viewers and profit only, with pinned SQL Server reference values and versioned Domain formula rules in `balance.md`.
- Load game parameters from the run's pinned published catalogue. Use its final week and threshold at the documented checkpoints, and reject further commands for a terminal run. A new catalogue version cannot change an active run's ending rule or balance constants.
- Validate player-entered streamer names at `StartRun`; escape them when rendering. Keep generated suggestions and any other setup randomness separate from the saved gameplay PRNG.
- Keep diagnostic log fields and metric labels bounded and free of player text or full save bodies. Count an accepted gameplay command once on immediate retry where possible; treat aggregate analytics as approximate and keep its writer independent of the pure Domain transition.
- Validate untrusted state, event IDs, options, and JSON size; return stable error codes for invalid choice, unaffordable action, unavailable catalogue version, and incompatible save.
- Keep published catalogue versions available as long as compatible saves may reference them; document any retirement/migration policy.
- The server and client expose one compatible API contract version. Do not leak EF entities or domain aggregates through HTTP.
- The test projects and quality gates are in [testing strategy](testing-strategy.md).
