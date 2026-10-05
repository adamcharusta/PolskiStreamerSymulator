# Testing strategy and project plan

The current solution has no test projects. Create tests alongside the first behavior they verify, rather than adding empty projects only to match the architecture. Use one test framework consistently; **xUnit** is the proposed default because it integrates with `dotnet test` and the other recommended tools. [Microsoft's xUnit guide](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-csharp-with-xunit) shows the basic project setup.

## Proposed test projects

| Project | References | Test purpose |
| --- | --- | --- |
| `tests/Domain.Tests` | `Domain` | Fast deterministic rules and property-style invariants |
| `tests/Application.Tests` | `Application`, `Domain` | CQRS handler orchestration with fake ports and controlled RNG |
| `tests/Infrastructure.IntegrationTests` | `Infrastructure`, `Application`, `Domain` | Real SQL Server catalogue and analytics databases, migrations, import and published-version behavior |
| `tests/Server.IntegrationTests` | `Server`, `Contracts` | Stateless HTTP contract, DI composition, Wolverine handler discovery, error mapping |
| `tests/BlazorApp.ComponentTests` | `BlazorApp`, `Contracts` | Component behavior, browser-save adapter, and accessible form feedback with bUnit |
| `tests/E2E.Tests` | No production project reference required | Browser flow across the hosted app with Playwright for .NET |

`Domain.Tests` and `Application.Tests` should run on every change. Provider and server integration tests should run in CI and before merging persistence or API changes. Browser E2E tests cover a few critical journeys, not every arithmetic branch. Balance sweeps can live in `Domain.Tests` under a separate trait and run before release or nightly, so a 1,000-seed test does not slow every edit.

## High-value test cases

### Domain

- The same seed and choices produce the same report, event, and resulting RNG state.
- A saved run pins its code rules version and SQL Server game-catalogue version; a later publication does not change its run length, starting values, bands, income/score parameters, name, or the outcome of the same saved choice.
- Viewers never become negative, drama stays within 0–100, and money changes by the stated amount.
- Every monetary component has a categorized ledger entry, with no unclassified money delta; `openingMoney + sponsors + donations + subscriptions - expenses == closingMoney` for each completed week and the full run.
- Subscription revenue is settled once per week, including quiet weeks and weeks with a pending event, and is not paid again after the event response or retry.
- The proposed subscription formula yields 0, 2, 10, 25, and 100 PLN for post-action audiences of 0, 20, 100, 250, and 1,000 under the first SQL Server divisor of 10. A second published divisor changes only new runs. An event's later viewer change affects the next week, not the already settled payment.
- An event's automatic encounter cost is charged only if that event is selected, once before presenting responses. An option cost is a separate expense charged only for the chosen response; both survive a pending save and retry without a duplicate charge.
- After an automatic encounter cost, unaffordable paid responses are unavailable and a free response remains selectable unless that cost ends the run in bankruptcy. An unavoidable encounter cost may put money below zero or reach the loss limit without corrupting the ledger.
- With the initial pinned -1,000 PLN threshold, -999 PLN remains active and -1,000 PLN or less ends in defeat. Test each checkpoint: weekly action plus subscriptions, automatic encounter cost, and response cost plus outcome. Multi-entry cash flows are reconciled before each check; the order of entries inside one step cannot change the result. A later published threshold affects new runs only.
- Bankruptcy stops further rolls, choices, and subscription payments. A defeat during week 52 takes priority over ordinary completion. The recap records the cause and a score, but its status remains defeat; retries cannot repeat the cost or roll that caused it.
- Exactly one weekly action is selected; it and each event response have weighted outcomes summing to 10,000 basis points. Boundary rolls select exactly one expected interval and guaranteed costs are paid once.
- Calm, middle, and high-drama eligibility, money/viewer thresholds, per-event encounter rolls, outcome rolls, collision selection, and cooldown use the correct state and stable ID order.
- A one-time viewer milestone is awarded once, including when an event crosses its threshold, if milestones remain in the final rules.
- The configured final week, initially 52, produces a recap with one career score computed from final viewers and net profit, plus money, drama, and run history. A short test configuration (for example four weeks) finishes at its own last week, not week 52. Changing only drama cannot change the score; changing an income or expense entry changes net profit and produces the component points specified in `balance.md`, allowing for integer rounding.
- Score examples cover zero profit, positive profit, a loss, the pinned starting balance, rounding, zero-score floor, alternate published reference values, and bankruptcy recaps. Ledger totals and `finalMoney - StartingMoneyPln` agree, and the displayed component points add to the displayed score unless the zero floor applies.
- Across generated valid states and choices, resolving a week either returns a valid state or a documented validation error, never a partial mutation.

### Application

- `GetSetupOptions` exposes the latest published parameters and one name suggestion. `StartRun` pins the version shown during setup, even if a newer version was published before submission, and writes no player record.
- A regenerated two-part name suggestion does not consume gameplay PRNG state. A valid custom name replaces the suggestion and survives browser save/load; an invalid name returns a stable validation error.
- `PlanWeek` validates incoming local state, loads its pinned catalogue version, and invokes the domain transition once.
- `ChooseEventOption` uses the pending state and selected option to produce exactly one report.
- Active or pending client state already at or below its pinned bankruptcy limit is rejected; completed and bankrupt runs reject further gameplay commands.
- The same state and command return the same result on retry; commands never persist a run on the server.
- Catalogue queries return read models without exposing EF entities.
- Validator tests cover unavailable choices, unaffordable guaranteed costs, invalid percentage totals, and a response that does not belong to the pending event.

### Infrastructure and server

- Weekly-action definitions and outcomes, event definitions and outcomes, every typed game parameter, and both streamer-name part groups survive a real SQL Server round trip.
- Draft import rejects duplicate IDs, retired statistic names, uncategorized money effects, negative guaranteed/encounter/outcome cost magnitudes, events without a free response, invalid drama-band/threshold conditions, one-outcome choices, and weekly-action or event-response chances that do not sum to 10,000.
- Convert and validate the 20 examples in `sample-events.md` as a content fixture before any are published; check response weights, free fallback after encounter costs, and coverage across the three drama bands.
- Publishing freezes a catalogue version, including parameters and name parts; a later draft cannot change a version used by an existing browser save. Missing or duplicate parameter rows, invalid ranges or cross-field relationships, missing name-part groups, duplicate part IDs/text, and names that exceed the assembled length limit fail import.
- A migration can create and update a fresh event-catalogue database.
- The server starts with all handlers discoverable; one HTTP command reaches a Wolverine handler.
- Invalid state, invalid option, oversized payload, and unavailable catalogue version return stable status and Problem Details codes.
- Server requests do not create player or run rows in SQL Server, and request logging omits local save content and names.

Use the actual production database provider for persistence integration tests where practical. EF Core [discourages its InMemory provider as a relational database substitute](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy). For API integration, use [WebApplicationFactory](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0) with controlled configuration and a test database.

### Components and E2E

- A setup form has no archetype field, shows the configured starting stats and run length, lets the player regenerate or replace the suggested name, displays validation feedback, and submits only one command per user action.
- Weekly and event choice screens show guaranteed costs, all weighted outcomes, the bankruptcy limit, and keyboard focus; the event screen distinguishes an already paid encounter cost from an optional response cost. A result matches the chosen option and recorded roll. The report displays four cash subtotals that reconcile to the money change. A loss caused by an automatic encounter cost shows no response screen.
- Browser save round trips preserve a pending event and completed week; corrupted or incompatible slots remain untouched with a clear error.
- A full browser path covers start → plan week → event choice → report → local save → reload → resume → year recap, including the score and its audience/net-profit breakdown.
- A separate browser path checks the 320 CSS pixel layout and keyboard-only operation.

[bUnit](https://bunit.dev/docs/getting-started/) is well suited to isolated Razor component tests. [Playwright for .NET](https://playwright.dev/dotnet/docs/intro) covers real browser behavior and reload/persistence. Keep E2E selectors semantic (`GetByRole`, labels) so tests also expose accessibility problems.

### Logging and aggregate analytics

- A successful `StartRun`, weekly action, event encounter/presentation, response, and terminal result increments the intended aggregate buckets by catalogue version and week. The initial app-open request counts a page load; a reload counts another open rather than claiming a unique player.
- Immediate retries of the same gameplay command are counted once while the bounded in-memory dedup cache is live. A restart can make counts approximate, and reports label them accordingly.
- A failing or full analytics writer logs a bounded warning, increments a drop metric, and does not change a gameplay response or browser save. The catalogue remains usable if `PssAnalytics` is unavailable.
- Neither structured logs nor analytics tables contain streamer/channel names, request bodies, browser saves, IP/user-agent strings, raw run IDs, or free-text exception messages. Metric labels use route templates and fixed low-cardinality IDs.
- An allowlisted client-error report is rate-limited; invalid codes and oversized bodies are rejected. Operator reports are not reachable through the public game ingress.
- Daily opens, most common weekly actions and event responses, ending-week distribution, and week progression funnel return the expected totals from a small synthetic dataset.

## Package shortlist

| Need | Proposed package | When to add |
| --- | --- | --- |
| General .NET tests | `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` | First test project |
| Code coverage | `coverlet.collector` | CI coverage reporting, after useful tests exist |
| Blazor components | `bunit` | First interactive component with meaningful behavior |
| Browser journeys | `Microsoft.Playwright.Xunit` | First complete hosted flow |
| ASP.NET Core integration | `Microsoft.AspNetCore.Mvc.Testing` | Server project exists |
| SQL Server integration database | SQL Server Express container with `Microsoft.EntityFrameworkCore.SqlServer`; two fresh test databases | First catalogue or analytics repository test; run in CI with a container runtime |
| Property-based checks | `FsCheck.Xunit` or equivalent | After domain transition API stabilizes |

Keep packages centrally versioned in `Directory.Packages.props`. Do not add a mocking library by default: hand-written fake repositories and deterministic RNGs are small and make test behavior obvious. Add one only if tests show repeated setup cost. Avoid snapshot tests for numeric game rules; explicit assertions and invariant checks give clearer failures.
