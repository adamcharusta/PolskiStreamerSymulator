# Decisions and open questions

This is the canonical record of choices that affect scope or implementation. **Confirmed** means the creator stated it. **Proposed** means a working default in these documents. **Open** means implementation is blocked or materially affected until resolved.

| ID | Topic | Status | Current position / next decision |
| --- | --- | --- | --- |
| D-01 | Game title | Confirmed | Polski Streamer Symulator. |
| D-02 | Documentation language | Confirmed | Documentation, `CLAUDE.md`, and `AGENTS.md` are in English. |
| D-03 | Turn length | Confirmed | One player turn represents one streaming week. |
| D-04 | Tone | Confirmed | Satire of Polish internet culture. |
| D-05 | Technical stack | Confirmed | Existing solution targets .NET 10 with standalone Blazor WebAssembly, C#, central package management, and the Domain/Application/Infrastructure/BlazorApp split. |
| D-06 | Player-facing language | Proposed | Polish. Confirm if bilingual UI is desired. |
| D-07 | Run duration | Confirmed | The first published game configuration has 52 weekly turns. `RunLengthWeeks` is stored in the versioned SQL Server parameters so later published versions can use another duration; an active run keeps its duration. |
| D-08 | Business model | Proposed | Free, no ads, no accounts, no in-game purchases for MVP. Confirm any funding or monetization requirement. |
| D-09 | Save model | Confirmed | Career saves stay in the player's browser; no account or cross-device sync. Three manual slots plus autosave remain a proposed UI detail. |
| D-10 | Core statistics and setup | Confirmed | Only money, channel viewers, and drama are core game statistics. The first published values start at 1,500 PLN, 20 viewers, and drama 50 on a 0–100 scale. No starting archetypes. These initial numbers are versioned SQL Server parameters. |
| D-11 | Real-world references | Proposed | Fictional platforms, people, and brands; satirical situations rather than direct portrayals. |
| D-12 | Online services | Proposed | An ASP.NET Core server is confirmed; no real platform APIs or third-party browser trackers are proposed. First-party aggregate analytics are now requested and designed in `observability-and-analytics.md`. |
| D-13 | Architecture | Confirmed | CQRS with Domain for game domains, Application for use cases, Infrastructure for adapters, and BlazorApp for views. |
| D-14 | Server | Confirmed | Add an ASP.NET Core server. The proposed graph also adds a small shared `Contracts` project. |
| D-15 | Mediator | Proposed | WolverineFX on the server in mediator-only mode for the MVP. |
| D-16 | Mapping | Proposed | Mapster at the API boundary when mappings become nontrivial. |
| D-17 | Validation | Proposed | FluentValidation for Application commands; built-in Blazor form feedback and Domain invariants remain. |
| D-18 | Database purpose and engine | Proposed | Replace the earlier SQLite proposal with one SQL Server 2025 Express instance hosting separate `PssCatalog` and `PssAnalytics` databases. The catalogue stores shared events, typed parameters, and name parts; analytics stores aggregate counters only. Neither stores players, chosen names, or career saves. Confirm the engine switch before provisioning production. |
| D-19 | Database access | Proposed | EF Core 10 with `Microsoft.EntityFrameworkCore.SqlServer` in Infrastructure; two DbContexts and separate migrations. A versioned published catalogue prevents content edits from changing an active run. |
| D-20 | Deployment target | Confirmed | Single Ubuntu 24.04 VPS (4 vCPU, 8 GB RAM, 75 GB disk) at `polskistreamersymulator.pl`, with single-node k3s, Ansible provisioning, and manually triggered GitHub Actions deployment. Detailed topology and safeguards are in `deployment.md`. |
| D-21 | Production persistence | Proposed | One SQL Server Express StatefulSet with a dedicated persistent volume and internal ClusterIP service; one application replica with no database volume. Use dedicated runtime credentials and encrypted off-VPS database backups before public release. |
| D-22 | Dice-driven choices | Confirmed | Every player choice, including weekly choices and event responses, has percentage-based consequences. Guaranteed costs are stated separately. |
| D-23 | Event eligibility | Confirmed | Money, viewers, and drama affect which events can appear. Provide calm, middle, and high-drama content. First proposed band boundaries are in `balance.md` and are typed SQL Server parameters. |
| D-24 | Event volume | Confirmed | Target at least 200 original events later; document about 20 examples now. `sample-events.md` contains 20 draft examples, not approved published content. A smaller representative fixture is sufficient to build the engine. |
| D-25 | Weekly action menu | Open | The creator confirmed exactly one action at the start of each week with weighted outcomes. The action list, costs, odds, and effects remain open; the proposed storage is the versioned SQL Server catalogue. The old separate format/effort selections are retired. |
| D-26 | Bankruptcy ending | Confirmed | In the first configuration, a money balance of -1,000 PLN or less ends the run immediately in defeat. The threshold is a typed parameter in the run's pinned published SQL Server catalogue. `game-design.md` defines the three atomic checkpoints and defeat recap. |
| D-27 | Money breakdown | Confirmed | One money balance with four cash-flow categories: sponsors, donations, subscriptions, and expenses. Subscription income is settled once each week; there is no separate active-subscriber statistic in the first version. |
| D-28 | Subscription formula | Proposed | Settle `floor(postActionViewers / SubscriptionViewersPerPln)` PLN once per week, including quiet weeks, with no extra roll or direct drama multiplier. The first proposed SQL Server divisor is 10; tune it through simulations. |
| D-29 | Event costs | Confirmed | Events may have an automatic cost on encounter and a separate cost for a chosen response, in addition to percentage-based outcome rolls. Both are expense entries charged once. |
| D-30 | Career score inputs | Confirmed | One final result uses only final channel viewers and net profit earned during the run, weighted equally at 50% each. Net profit is sponsor + donation + subscription income minus all expenses; the pinned starting balance (initially 1,500 PLN) does not count. Drama has no direct score term. |
| D-31 | Score normalization | Proposed | Use the uncapped square-root code formula in `balance.md`, with first proposed SQL Server values of 1,000 reference viewers, 5,000 PLN reference profit, and 500 points per component. Tune through simulations; pin data and algorithm versions separately. |
| D-32 | First event examples | Confirmed | The first documentation pass has 20 complete draft examples in `sample-events.md`; they satisfy the earlier request for a representative sample but are not approved SQL Server content. |
| D-33 | Configurable parameters | Confirmed | Store run length and other tunable numeric values in one typed `GameParameters` row per published SQL Server catalogue version. An active run pins its version. The initial field list and validation are in `event-system.md`; gameplay formulas remain in Domain code. |
| D-34 | Streamer name generator | Confirmed | Suggest a streamer name by concatenating two parts from the versioned SQL Server vocabulary. The player may replace it with a custom name. Neither the chosen name nor any player record enters SQL Server. Initial word lists and name-validation details in `streamer-name-generation.md` are proposed. |
| D-35 | Weekly-action catalogue | Proposed | Put weekly-action definitions, costs, and weighted outcomes in the same versioned SQL Server catalogue so their tuning does not require a code deployment. The action menu itself remains open. |
| D-36 | Logging and statistics | Confirmed | Record errors and operational information; measure app opens, where careers end, and the most common player choices. A page open is not a unique person, and abandonment can only be estimated without server-side run saves. |
| D-37 | Telemetry implementation | Proposed | Use JSON console logs and built-in .NET operational metrics; store only first-party aggregate gameplay counters in the separate `PssAnalytics` SQL Server database. No external tracker or public analytics dashboard. The retention, privacy review, and operator reports are in `observability-and-analytics.md`. |

## Questions to resolve before or during implementation

1. Should event authors update a draft through an import tool first, or is an authenticated admin UI needed in the first release? The proposed MVP uses an import/publish tool.
2. Which actions should be available each week, and what are their first costs and outcome distributions? The player makes one weekly action choice.
3. Is there a preferred business model or publication venue? This affects product constraints and operations.
4. How sharp should the satire be, and which topics should be excluded from the event catalogue?
5. Should the player start with a fictional platform choice, or should platform differences wait for a later version? The current proposal defers them.
6. Which encrypted off-VPS backup destination and retention period will be used before public release?
7. Does the proposed one-PLN-per-ten-viewers weekly subscription payout keep money meaningful alongside sponsor, donation, and expense ranges over 52 weeks?
8. Do the provisional score reference values give audience and net profit comparable influence across representative 52-week careers?
9. Confirm the proposed analytics retention and privacy notice before telemetry is enabled in production.
10. Confirm SQL Server 2025 Express as the production database engine and verify VPS memory/disk headroom under a representative workload before enabling the database workload.

## Decision procedure

When the creator answers a question, update its row and the authoritative document in the same change. If a playtest overturns a proposed default, record the new decision and why. Keep old rationale in version control rather than leaving contradictory rules in multiple documents.
