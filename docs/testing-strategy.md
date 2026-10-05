# Testing strategy and project plan

The current solution has no test projects. Create tests alongside the first behavior they verify, rather than adding empty projects only to match the architecture. Use one test framework consistently; **xUnit** is the proposed default because it integrates with `dotnet test` and the other recommended tools. [Microsoft's xUnit guide](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-csharp-with-xunit) shows the basic project setup.

## Proposed test projects

| Project | References | Test purpose |
| --- | --- | --- |
| `tests/Domain.Tests` | `Domain` | Fast deterministic rules and property-style invariants |
| `tests/Application.Tests` | `Application`, `Domain` | CQRS handler orchestration with fake ports and controlled RNG |
| `tests/Infrastructure.IntegrationTests` | `Infrastructure`, `Application`, `Domain` | Real SQLite event catalogue, migrations, import and published-version behavior |
| `tests/Server.IntegrationTests` | `Server`, `Contracts` | Stateless HTTP contract, DI composition, Wolverine handler discovery, error mapping |
| `tests/BlazorApp.ComponentTests` | `BlazorApp`, `Contracts` | Component behavior, browser-save adapter, and accessible form feedback with bUnit |
| `tests/E2E.Tests` | No production project reference required | Browser flow across the hosted app with Playwright for .NET |

`Domain.Tests` and `Application.Tests` should run on every change. Provider and server integration tests should run in CI and before merging persistence or API changes. Browser E2E tests cover a few critical journeys, not every arithmetic branch. Balance sweeps can live in `Domain.Tests` under a separate trait and run before release or nightly, so a 1,000-seed test does not slow every edit.

## High-value test cases

### Domain

- The same seed and choices produce the same report, event, and resulting RNG state.
- Every metric stays within its declared bounds; followers never become negative.
- A break restores energy and uses the correct audience drift; intensive work at low energy applies its penalty.
- Event eligibility, per-event occurrence rolls, outcome rolls, collision selection, and four-week cooldown use the correct state and stable ID order.
- A milestone is awarded once, including when an event crosses its threshold.
- Week 52 produces a final recap; four consecutive weeks below the debt floor produce the early ending.
- Across generated valid states and choices, resolving a week either returns a valid state or a documented validation error, never a partial mutation.

### Application

- `StartRun` pins the latest published catalogue version without writing a player record.
- `PlanWeek` validates incoming local state, loads its pinned catalogue version, and invokes the domain transition once.
- `ChooseEventOption` uses the pending state and selected option to produce exactly one report.
- The same state and command return the same result on retry; commands never persist a run on the server.
- Catalogue queries return read models without exposing EF entities.
- Validator tests cover conditional choices such as promotion during a break and unaffordable upgrades.

### Infrastructure and server

- Event definitions, conditions, options, outcomes, and effects survive a real SQLite round trip.
- Draft import rejects duplicate IDs, invalid condition/effect types, and outcome chances that do not sum to 10,000.
- Publishing freezes a catalogue version; a later draft cannot change a version used by an existing browser save.
- A migration can create and update a fresh event-catalogue database.
- The server starts with all handlers discoverable; one HTTP command reaches a Wolverine handler.
- Invalid state, invalid option, oversized payload, and unavailable catalogue version return stable status and Problem Details codes.
- Server requests do not create player or run rows in SQLite, and request logging omits local save content and names.

Use the actual production database provider for persistence integration tests where practical. EF Core [discourages its InMemory provider as a relational database substitute](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy). For API integration, use [WebApplicationFactory](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0) with controlled configuration and a test database.

### Components and E2E

- A setup form displays validation feedback, disables an unaffordable action, and submits only one command per user action.
- The event screen shows direct costs, keyboard focus, and a result that matches the chosen option.
- Browser save round trips preserve a pending event and completed week; corrupted or incompatible slots remain untouched with a clear error.
- A full browser path covers start → plan week → event choice → report → local save → reload → resume → year recap.
- A separate browser path checks the 320 CSS pixel layout and keyboard-only operation.

[bUnit](https://bunit.dev/docs/getting-started/) is well suited to isolated Razor component tests. [Playwright for .NET](https://playwright.dev/dotnet/docs/intro) covers real browser behavior and reload/persistence. Keep E2E selectors semantic (`GetByRole`, labels) so tests also expose accessibility problems.

## Package shortlist

| Need | Proposed package | When to add |
| --- | --- | --- |
| General .NET tests | `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` | First test project |
| Code coverage | `coverlet.collector` | CI coverage reporting, after useful tests exist |
| Blazor components | `bunit` | First interactive component with meaningful behavior |
| Browser journeys | `Microsoft.Playwright.Xunit` | First complete hosted flow |
| ASP.NET Core integration | `Microsoft.AspNetCore.Mvc.Testing` | Server project exists |
| SQLite integration database | `Microsoft.EntityFrameworkCore.Sqlite` with a temporary database/connection | First event catalogue repository test |
| Property-based checks | `FsCheck.Xunit` or equivalent | After domain transition API stabilizes |

Keep packages centrally versioned in `Directory.Packages.props`. Do not add a mocking library by default: hand-written fake repositories and deterministic RNGs are small and make test behavior obvious. Add one only if tests show repeated setup cost. Avoid snapshot tests for numeric game rules; explicit assertions and invariant checks give clearer failures.
