# Technical design: .NET, CQRS, and event content

## Observed solution and approved direction

The repository contains a .NET 10 solution under `PolskiStreamerSymulatorApp/`. `global.json` requests SDK `10.0.201` with `latestFeature` roll-forward; the installed `10.0.401` SDK built the original skeleton successfully on 2026-10-04. Current projects are `Domain`, `Application`, `Infrastructure`, and standalone Blazor WebAssembly `BlazorApp`. Central package management and warnings-as-errors are enabled.

The creator confirmed a CQRS architecture and an **ASP.NET Core server**. The server hosts the WebAssembly client, runs Wolverine handlers, and reads a SQLite catalogue of possible in-game events. **Career state and saves stay in the browser; no player or run records go into SQLite.** The server and contract projects below are target architecture, not existing code.

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
| `Application` | CQRS commands/queries and handlers, game use cases, `IEventCatalog` port, command validators | EF entities, UI, browser storage |
| `Infrastructure` | EF Core SQLite event catalogue, migrations, catalogue repository, content import/publish tooling | Player saves, gameplay formulas |
| `Contracts` | Versioned API request/response records, save DTO, stable error codes | Domain behavior and database entities |
| `Server` | Host and DI composition, Wolverine configuration, HTTP endpoints, size limits, transport error mapping, static client hosting | Gameplay formulas or player persistence |
| `BlazorApp` | Polish components, screen state, API client, browser save adapter, accessibility | SQLite access or trusted gameplay rules |

`Infrastructure` registers its adapters; `Server` composes the full application. Refactor the current empty registration extensions in `Application` and `Infrastructure` to extend `IServiceCollection` rather than `WebAssemblyHostBuilder`, then remove their `Microsoft.AspNetCore.Components.WebAssembly` references. `BlazorApp` should reference only `Contracts` among the project libraries.

## CQRS flow and local career state

Commands express intent: `StartRun`, `PlanWeek`, and `ChooseEventOption`. A catalogue query such as `GetPublishedCatalogVersion` supports startup and diagnostics. Dashboard and history are projections of the browser's saved run state; do not send a server query for data already in that state. Use feature folders in `Application`, with each command, handler, validator, and result together.

1. `StartRun` loads the latest **published** event-catalogue version and returns a new `RunStateDto` with seed, RNG state, schema version, and pinned catalogue version.
2. `PlanWeek` receives the current `RunStateDto` and weekly choice. The server validates size/schema/IDs and metric bounds, reconstructs a domain state, loads the pinned event catalogue from SQLite, invokes the pure simulation, and returns a new state. Since earlier turns are not stored server-side, the server cannot prove that client-supplied historical values are authentic. If an event needs a choice, the result contains `pendingWeek`.
3. Blazor writes a versioned save in browser storage before displaying a pending event. `ChooseEventOption` receives that pending state and option ID, applies it once, and returns the completed report and state. Blazor saves it before advancing the UI.

The server does **not** retain run state between requests. The same input state, catalogue version, and choice must produce the same result, which makes a retry safe. The UI must disable duplicate submission and reject a response for an older local week. Because the player owns the local save, they can alter it; this is acceptable for a single-player game without rankings. The server still validates untrusted input, bounds request size, and never logs the state body or player-entered names. If server-authoritative saves, multiplayer, or leaderboards are added later, this protocol must be redesigned.

The `RunStateDto` needs `schemaVersion`, `catalogVersion`, `runId`, seed/RNG algorithm and state, player setup, week/status, metrics, debt counter, milestone IDs, pending week, and history. The domain model is reconstructed through validation methods, not blindly mapped from arbitrary JSON. Store event and option IDs plus result deltas in history so later text edits do not rewrite an old report.

## Wolverine, Mapster, and FluentValidation

- **WolverineFX:** Configure it in `Server`, explicitly discover handlers in `Application`, and use `IMessageBus.InvokeAsync` for request/response. Start in [mediator-only mode](https://wolverinefx.net/tutorials/mediator.html); no queue, outbox, or per-player message persistence is needed. Review [assembly discovery](https://wolverinefx.net/guide/handlers/discovery) because handlers live outside the host project.
- **Mapster:** Use it for simple API DTO to Application command/result mappings at the server boundary. Never use a mapper to bypass domain validation when rebuilding `RunState`. Centralize and test nontrivial mappings. The [official project](https://github.com/MapsterMapper/Mapster) also supports generated mappings.
- **FluentValidation:** Validate command shape and cross-field input rules in `Application`; `Domain` still protects invariants. Wolverine has [FluentValidation middleware](https://wolverinefx.net/guide/handlers/fluent-validation). Choose one validator-registration path, and return stable 400 Problem Details responses for invalid API input. The old `FluentValidation.AspNetCore` automatic MVC integration [does not apply to Blazor or Minimal APIs](https://docs.fluentvalidation.net/en/latest/aspnet.html). Blazor's own form validation provides immediate UI feedback.

Add Mapster and FluentValidation when the first real mapping and validator exist. Keep package versions in `Directory.Packages.props` and check compatibility with the solution's .NET version.

## SQLite is an event catalogue

SQLite stores **content shared by all players**: event IDs, Polish narrative copy, eligibility rules, option labels, occurrence chances, outcome chances, and metric/cash effects. It stores no names, followers, run history, or save slots. EF Core models and migrations belong to `Infrastructure`. Gameplay should query a published catalogue version and evaluate typed rules in `Domain`; it must not execute SQL expressions or arbitrary code supplied by content rows. See [event system](event-system.md) for the schema and publishing workflow.

An active run pins one immutable catalogue version. Editing a draft and publishing a new version affects new runs, while an old version remains available for existing local saves. The server may cache an immutable published catalogue per version and invalidate only when a new version is published. The database file and backups must persist across deployments. Choose an ASP.NET Core host accordingly; the provider's [SQLite limitations](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations) apply to content migrations.

## Browser saves and deployment

Use a small `IBrowserSaveStore` abstraction in `BlazorApp`, implemented with JS interop and browser `localStorage` for the initial 52-week run. Reserve one autosave and up to three named manual slots. Serialize the complete versioned state to one key per slot, validate it before writing, catch quota/storage errors, and show a failure instead of claiming success. On load, validate schema and catalogue version; an incompatible save remains untouched with a clear message. Add export/import later if players need transfer or backup. Browser storage is per browser profile and can be cleared by the user.

Deploy the client and API on one HTTPS origin. The server needs persistent SQLite storage, a content migration/publishing procedure, and a backup/restore plan. The browser never receives database credentials. Do not add accounts, player cookies, or server-side player records for the MVP. The selected single-VPS k3s topology, provisioning, release procedure, and current implementation gates are in [deployment](deployment.md).

## Technical quality requirements

- Domain simulation has no ambient clock, `Random.Shared`, database, or browser dependency. Persist the PRNG algorithm/version and state in the local save.
- Sort eligible event IDs before probability rolls so database row order cannot change a seeded run.
- Validate untrusted state, event IDs, options, and JSON size; return stable error codes for invalid choice, unaffordable action, unavailable catalogue version, and incompatible save.
- Keep published catalogue versions available as long as compatible saves may reference them; document any retirement/migration policy.
- The server and client expose one compatible API contract version. Do not leak EF entities or domain aggregates through HTTP.
- The test projects and quality gates are in [testing strategy](testing-strategy.md).
