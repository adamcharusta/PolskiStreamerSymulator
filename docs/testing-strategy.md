# Testing strategy and project plan

The solution's first test project is `tests/Server.IntegrationTests`. Create further test projects alongside the first behavior they verify, rather than adding empty projects only to match the architecture. Use one test framework consistently: **xUnit**, through the `xunit.v3` package. [Microsoft's xUnit guide](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-csharp-with-xunit) shows the basic project setup.

## Test platform and commands

Tests run on Microsoft Testing Platform v2, the default of the `xunit.v3` 4.x packages. `PolskiStreamerSymulatorApp/global.json` switches `dotnet test` to its Microsoft Testing Platform mode, and `dotnet` reads that file only when a command runs from `PolskiStreamerSymulatorApp/` or below. Run the tests from that folder:

    dotnet test --solution PolskiStreamerSymulatorApp.sln

In this mode, pass a solution with `--solution` and a single project with `--project`. Running `dotnet test` from the repository root falls back to the VSTest mode, which cannot run these projects. Each test project keeps `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio`, as xUnit recommends, so IDE test explorers that still use VSTest can discover the tests. Test projects are executables and set `<OutputType>Exe</OutputType>`.

## Proposed test projects

| Project | References | Test purpose |
| --- | --- | --- |
| `tests/Domain.Tests` | `Domain` | Fast deterministic rules and property-style invariants |
| `tests/Application.Tests` | `Application`, `Domain` | CQRS handler orchestration with fake ports and controlled RNG |
| `tests/Infrastructure.IntegrationTests` | `Infrastructure`, `Application`, `Domain` | Real SQLite catalogue and analytics databases, migrations, draft validation, audit, and published-version behavior |
| `tests/Server.IntegrationTests` | `Server`, `Contracts` | Stateless gameplay HTTP contract, admin authentication and authorization, DI composition, Wolverine handler discovery, error mapping |
| `tests/BlazorApp.ComponentTests` | `BlazorApp`, `Contracts` | Player and admin component behavior, browser-save adapter, and accessible form feedback with bUnit |
| `tests/E2E.Tests` | No production project reference required | Browser flow across the hosted app with Playwright for .NET |

`Server.IntegrationTests` exists since work package F1. Add the other projects with the first behavior they verify.

`Domain.Tests` and `Application.Tests` should run on every change. Provider and server integration tests should run in CI and before merging persistence or API changes. Browser E2E tests cover a few critical journeys, not every arithmetic branch. Balance sweeps can live in `Domain.Tests` under a separate trait and run before release or nightly, so a 1,000-seed test does not slow every edit.

## High-value test cases

### Domain

- The same seed and choices produce the same report, event, and resulting RNG state.
- A saved run pins its code rules version and SQLite game-catalogue version; a later publication does not change its run length, starting values, bands, income/score parameters, name, or the outcome of the same saved choice.
- Viewers never become negative, drama stays within 0–100, and money changes by the stated amount.
- Every monetary component has a categorized ledger entry, with no unclassified money delta; `openingMoney + sponsors + donations + subscriptions - expenses == closingMoney` for each completed week and the full run.
- Subscription revenue is settled once per week, including quiet weeks and weeks with a pending event, and is not paid again after the event response or retry.
- The proposed subscription formula yields 0, 2, 10, 25, and 100 PLN for post-action audiences of 0, 20, 100, 250, and 1,000 under the first SQLite divisor of 10. A second published divisor changes only new runs. An event's later viewer change affects the next week, not the already settled payment.
- An event's automatic encounter cost is charged only if that event is selected, once before presenting responses. An option cost is a separate expense charged only for the chosen response; both survive a pending save and retry without a duplicate charge.
- A paid weekly action or event response can be selected with less cash than its guaranteed cost, including a cost that temporarily drops money to or below the bankruptcy limit. The full rolled action or response step can recover above the threshold; a failed recovery ends in bankruptcy after the atomic step. The expense is recorded exactly once. An unavoidable encounter cost retains its separate immediate bankruptcy check. A free response remains available if that cost did not already end the run.
- With the initial pinned -1,000 PLN threshold, -999 PLN remains active and -1,000 PLN or less ends in defeat. Test each checkpoint: weekly action plus subscriptions, automatic encounter cost, and response cost plus outcome. Multi-entry cash flows are reconciled before each check; the order of entries inside one step cannot change the result. A later published threshold affects new runs only.
- Bankruptcy or a special terminal outcome stops further event selection, choices, and subscription payments. Bankruptcy during week 52 takes priority over ordinary completion, and bankruptcy is the primary reason if the same response also carries a special terminal code. Each recap records its cause and a score; retries cannot repeat the cost or roll that caused it.
- Exactly one weekly action is selected; it and each event response have weighted outcomes summing to 10,000 basis points. Boundary rolls select exactly one expected interval and guaranteed costs are paid once.
- Calm, middle, and high-drama eligibility, money/viewer thresholds, per-event encounter rolls, outcome rolls, collision selection, and per-event repeat policy use the correct state and stable ID order. A one-time event never returns; a repeatable event cannot return before its cooldown elapses.
- A specific rolled parent outcome sets a narrative flag once, while an alternate outcome does not. The flag unlocks a later event only within its authored window; that event still needs to pass its own seeded encounter roll and collision selection. A follow-up result clears or replaces the flag; an expired flag cannot produce a late sequel. Replaying the same saved input does not duplicate a flag transition.
- A week can present one, two, or three distinct events under the first pinned cap of 3 and settle subscriptions only once. It never presents a fourth. Each event gets at most one encounter roll that week; passing unselected candidates may appear later if they remain eligible, and newly eligible candidates receive their first roll. A flag-gated follow-up has the same selection odds as an ordinary passing candidate. One full-week report includes every event and cash entry.
- A rolled event result with `permanent_ban` or `channel_closed` ends the career in defeat; `retired` ends it neutrally. Each recap preserves its score, reason, and pinned classification; other outcome branches keep the run active. Unknown terminal reasons or wrong first-version classifications fail catalogue publication. If the same outcome ends below the debt limit, show bankruptcy as the primary ending while retaining the complete outcome in history.
- Crossing a viewer threshold affects ordinary event eligibility but produces no separate milestone reward or consumed event slot.
- The configured final week, initially 52, produces a recap with one career score computed from final viewers and net profit, plus money, drama, and run history. A short test configuration (for example four weeks) finishes at its own last week, not week 52. Changing only drama cannot change the score; changing an income or expense entry changes net profit and produces the component points specified in `balance.md`, allowing for integer rounding.
- Score examples cover zero profit, positive profit, a loss, the pinned starting balance, rounding, zero-score floor, alternate published reference values, and bankruptcy recaps. Ledger totals and `finalMoney - StartingMoneyPln` agree, and the displayed component points add to the displayed score unless the zero floor applies.
- Across generated valid states and choices, resolving a week either returns a valid state or a documented validation error, never a partial mutation.

### Application

- `GetSetupOptions` exposes the latest published parameters and one name suggestion. `StartRun` pins the version shown during setup, even if a newer version was published before submission, and writes no player record.
- A regenerated two-part name suggestion does not consume gameplay PRNG state. A valid custom name replaces the suggestion and survives browser save/load; an invalid name returns a stable validation error.
- `PlanWeek` validates incoming local state, loads its pinned catalogue version, and invokes the domain transition once.
- `ChooseEventOption` uses the pending state and selected option to produce exactly one report.
- Active or pending client state already at or below its pinned bankruptcy limit is rejected; completed, bankrupt, and special-ending runs reject further gameplay commands.
- The same state and command return the same result on retry, including the next event selection and interim roll ledger; commands never persist a run on the server.
- Catalogue queries return read models without exposing EF entities.
- Validator tests cover unavailable choices, invalid percentage totals, and a response that does not belong to the pending event. A player-selected cost is not rejected merely because it temporarily crosses the bankruptcy limit.

### Infrastructure and server

- Weekly-action definitions and outcomes, event definitions and outcomes, every typed game parameter, and both streamer-name part groups survive a real SQLite round trip.
- Admin draft validation and any seed import reject duplicate IDs, retired statistic names, uncategorized money effects, negative guaranteed/encounter/outcome cost magnitudes, events without a free response, missing `pl-PL` or `en` text, invalid drama-band/threshold conditions, one-outcome choices, and weekly-action or event-response chances that do not sum to 10,000. Reject unknown flag IDs, a follow-up with no producing result, a same-week or impossible follow-up window, a follow-up result that leaves its triggering flag active, and invalid once/cooldown combinations.
- Convert and validate the 20 Polish examples in `sample-events.md` as a content fixture before any are published; add reviewed English text, then check response weights, free fallback after encounter costs, and coverage across the three drama bands.
- Publishing freezes a catalogue version, including parameters and name parts; a later draft cannot change a version used by an existing browser save. Missing or duplicate parameter rows, invalid ranges or cross-field relationships, missing name-part groups, duplicate part IDs/text, and names that exceed the assembled length limit block publication. Failed publication leaves the prior published version intact.
- Anonymous callers and GitHub users other than the allowlisted owner cannot read drafts or call any admin mutation or publication endpoint, including through a direct API request or the public hostname. The owner can edit, preview, and publish a draft. Authenticated edits and publication create audit entries with the owner's stable GitHub user ID and version; gameplay requests create none. Published versions reject edits even by the owner.
- Reject an invalid OAuth `state`, a callback to an unconfigured host, an expired admin session, and a cookie-authenticated mutation without valid antiforgery protection. The admin cookie is host-only and is not sent to the public game host. Sign-out ends the admin session.
- A migration can create and update a fresh event-catalogue database.
- A failed pre-publication or pre-migration backup blocks the change, while a successful consistent backup can restore both SQLite files and a pinned published catalogue version on a test machine. The scheduled daily backup reports transfer failure rather than claiming success.
- The server starts with all handlers discoverable; one HTTP command reaches a Wolverine handler.
- Invalid state, invalid option, oversized payload, and unavailable catalogue version return stable status and Problem Details codes.
- Server requests do not create player or run rows in SQLite, and request logging omits local save content and names.

Use the actual production database provider for persistence integration tests where practical. EF Core [discourages its InMemory provider as a relational database substitute](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy). For API integration, use [WebApplicationFactory](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0) with controlled configuration and a test database.

### Components and E2E

- A setup form has one streamer-name field and no archetype, channel-name, content-theme, or platform fields. It shows the configured starting stats and run length, lets the player regenerate or replace the suggested name, displays validation feedback in the selected language, and submits only one command per user action.
- Weekly and event choice screens show guaranteed costs, all weighted outcomes, the bankruptcy limit, and keyboard focus; the event screen distinguishes an already paid encounter cost from an optional response cost. A result matches the chosen option and recorded roll. The report displays four cash subtotals that reconcile to the money change. A loss caused by an automatic encounter cost shows no response screen.
- Browser save round trips preserve a pending event, current-week rolled/passing/selected IDs and event count, encounter history, active narrative flag and set week, and completed week across one autosave and three manual slots; corrupted or incompatible slots remain untouched with a clear localized error.
- Export then import a local save file into another browser profile and resume the same seeded career, including a week between its first and second events. Reject malformed, oversized, structurally inconsistent, or incompatible files without overwriting an existing slot; require confirmation before replacing a valid slot. The import/export path does not upload career content to server storage. A player can edit a valid local file in this single-player game, so this is validation rather than anti-cheat protection.
- A full browser path covers start → plan week → first event choice → interim result → second event choice → full weekly report → local save → reload → resume → career recap, including the score and its audience/net-profit breakdown.
- A separate browser path checks the 320 CSS pixel layout and keyboard-only operation. Switch from Polish to English during a pending event and after a completed week; the same pinned IDs, odds, numbers, and RNG state remain unchanged while all visible event and report text changes locale.
- A browser career covers a parent event, a later optional flagged follow-up, and a replay where the follow-up roll fails. The history links the two when both occurred and does not promise a sequel when none was selected.
- An admin browser path at `admin.polskistreamersymulator.pl` checks GitHub sign-in, bilingual draft editing, visible validation errors, preview, explicit owner publication, and sign-out. A signed-out browser cannot reach draft content through direct API calls.

[bUnit](https://bunit.dev/docs/getting-started/) is well suited to isolated Razor component tests. [Playwright for .NET](https://playwright.dev/dotnet/docs/intro) covers real browser behavior and reload/persistence. Keep E2E selectors semantic (`GetByRole`, labels) so tests also expose accessibility problems.

### Logging and aggregate analytics

- A successful `StartRun`, weekly action, event encounter/presentation, response, and terminal result increments the intended aggregate buckets by catalogue version and week. The initial app-open request counts a page load; a reload counts another open rather than claiming a unique player.
- Immediate retries of the same gameplay command are counted once while the bounded in-memory dedup cache is live. A restart can make counts approximate, and reports label them accordingly.
- A failing or full analytics writer logs a bounded warning, increments a drop metric, and does not change a gameplay response or browser save. The catalogue remains usable if `analytics.db` is unavailable.
- Neither structured logs nor analytics tables contain streamer/channel names, request bodies, browser saves, IP/user-agent strings, raw run IDs, or free-text exception messages. Metric labels use route templates and fixed low-cardinality IDs.
- An allowlisted client-error report is rate-limited; invalid codes and oversized bodies are rejected. Operator reports are not reachable through the public game ingress.
- Daily opens, most common weekly actions and event responses, ending-week distribution, and week progression funnel return the expected totals from a small synthetic dataset.
- The default aggregate app-open report works without a separate in-game opt-in control, while the player notice explains the collection. A retention job removes daily counters older than 12 months, node log configuration removes logs after 14 days, and the second machine deletes encrypted backups after 30 days. Restoring an older analytics snapshot re-enforces the counter limit before reports resume.

## Package shortlist

| Need | Proposed package | When to add |
| --- | --- | --- |
| General .NET tests | `xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` | Added with `Server.IntegrationTests` |
| Code coverage | `Microsoft.Testing.Extensions.CodeCoverage`; Coverlet's collector does not run under Microsoft Testing Platform | CI coverage reporting, after useful tests exist |
| Blazor components | `bunit` | First interactive component with meaningful behavior |
| Browser journeys | `Microsoft.Playwright.Xunit.v3` | First complete hosted flow |
| ASP.NET Core integration | `Microsoft.AspNetCore.Mvc.Testing` | Added with `Server.IntegrationTests` |
| SQLite integration database | `Microsoft.EntityFrameworkCore.Sqlite` with two isolated temporary database files | First catalogue or analytics repository test; run in CI without a database container |
| Property-based checks | `FsCheck.Xunit.v3` or equivalent | After domain transition API stabilizes |

Keep packages centrally versioned in `Directory.Packages.props`. Do not add a mocking library by default: hand-written fake repositories and deterministic RNGs are small and make test behavior obvious. Add one only if tests show repeated setup cost. Avoid snapshot tests for numeric game rules; explicit assertions and invariant checks give clearer failures.
