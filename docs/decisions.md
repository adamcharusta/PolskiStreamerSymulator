# Decisions and open questions

This is the canonical record of choices that affect scope or implementation. **Confirmed** means the creator stated it. **Proposed** means a working default in these documents. **Open** means implementation is blocked or materially affected until resolved.

| ID | Topic | Status | Current position / next decision |
| --- | --- | --- | --- |
| D-01 | Game title | Confirmed | Polski Streamer Symulator. |
| D-02 | Documentation language | Confirmed | Documentation, `CLAUDE.md`, and `AGENTS.md` are in English. |
| D-03 | Turn length | Confirmed | One player turn represents one streaming week. |
| D-04 | Tone | Confirmed | Satire of Polish internet culture. |
| D-05 | Technical stack | Confirmed | Existing solution targets .NET 10 with standalone Blazor WebAssembly, C#, central package management, and the Domain/Application/Infrastructure/BlazorApp split. |
| D-06 | Player-facing language | Confirmed | Ship Polish and English UI and event text in the first version. Documentation and code comments remain in English. |
| D-07 | Run duration | Confirmed | The first published game configuration has 52 weekly turns. `RunLengthWeeks` is stored in the versioned SQLite parameters so later published versions can use another duration; an active run keeps its duration. |
| D-08 | Business model | Confirmed | The first version is free, with no ads or in-game purchases. No player accounts are planned for browser-local saves. |
| D-09 | Save model | Confirmed | Career saves stay in the player's browser, with one autosave and three manual slots; no account or cloud sync. First-version export/import allows a player to move a save file manually between browsers or devices. |
| D-10 | Core statistics and setup | Confirmed | Only money, channel viewers, and drama are core game statistics. The first published values start at 1,500 PLN, 20 viewers, and drama 50 on a 0–100 scale. No starting archetypes. These initial numbers are versioned SQLite parameters. |
| D-11 | Real-world references | Confirmed | Use fictional names, brands, and platforms while writing situations that clearly evoke specific real internet stories involving public creators only. Do not make a private person the recognizable subject of an allusion. No additional blanket topic exclusions are planned; review sensitive allusions case by case. |
| D-12 | Online services | Proposed | An ASP.NET Core server is confirmed; no real platform APIs or third-party browser trackers are proposed. First-party aggregate analytics are now requested and designed in `observability-and-analytics.md`. |
| D-13 | Architecture | Confirmed | CQRS with Domain for game domains, Application for use cases, Infrastructure for adapters, and BlazorApp for views. |
| D-14 | Server | Confirmed | Add an ASP.NET Core server and a small shared `Contracts` project for HTTP DTOs. Both exist since work package F1. |
| D-15 | Mediator | Confirmed | Use WolverineFX on the server for CQRS dispatch. Start with mediator-only mode; add durable messaging only for a demonstrated need. |
| D-16 | Mapping | Confirmed | Use Mapster at the API boundary for DTO mappings. Do not bypass Domain validation. |
| D-17 | Validation | Confirmed | Use FluentValidation for Application commands; built-in Blazor form feedback and Domain invariants remain. |
| D-18 | Database purpose and engine | Confirmed | Use SQLite with EF Core for shared game content and aggregate analytics. Neither database stores players, chosen names, or career saves. |
| D-19 | Database access | Confirmed | Use EF Core 10 with `Microsoft.EntityFrameworkCore.Sqlite` in Infrastructure. A versioned published catalogue prevents content edits from changing an active run. The two-file/two-DbContext layout remains an infrastructure proposal in D-21. |
| D-20 | Deployment target | Confirmed | Single Ubuntu 24.04 VPS (4 vCPU, 8 GB RAM, 75 GB disk) at `polskistreamersymulator.pl`, with single-node k3s, Ansible provisioning, and manually triggered GitHub Actions deployment. Detailed topology and safeguards are in `deployment.md`. |
| D-21 | Production persistence | Proposed | Mount a local persistent volume with SQLite database files into the single application pod. No separate database server or Service is needed. Keep one app replica, run migrations/content publication as controlled maintenance steps, and encrypt database backups before sending them off the VPS. |
| D-22 | Dice-driven choices | Confirmed | Every player choice, including weekly choices and event responses, has percentage-based consequences. Guaranteed costs are stated separately. |
| D-23 | Event eligibility | Confirmed | Money, viewers, and drama affect which events can appear. Provide calm, middle, and high-drama content. First proposed band boundaries are in `balance.md` and are typed SQLite parameters. |
| D-24 | Event volume | Open | The creator is considering roughly 200–400 original events for the first public release, with the final target to be decided later. `sample-events.md` contains 20 Polish draft examples, not approved bilingual published content. A smaller representative fixture is sufficient to build and test the engine. |
| D-25 | Weekly action menu | Proposed | The creator accepted the four actions in `balance.md` as a working first menu, with costs and odds to be tuned after tests. Their final numbers are not yet confirmed. Store the menu in the versioned SQLite catalogue. |
| D-26 | Bankruptcy ending | Confirmed | In the first configuration, a money balance of -1,000 PLN or less ends the run immediately in defeat. The threshold is a typed parameter in the run's pinned published SQLite catalogue. `game-design.md` defines the settlement checkpoints and defeat recap; other special early endings are also allowed. |
| D-27 | Money breakdown | Confirmed | One money balance with four cash-flow categories: sponsors, donations, subscriptions, and expenses. Subscription income is settled once each week; there is no separate active-subscriber statistic in the first version. |
| D-28 | Subscription formula | Proposed | Use `floor(postActionViewers / SubscriptionViewersPerPln)` PLN once per week, including quiet weeks, with no extra roll or direct drama multiplier. The creator accepted 10 as a working first divisor; tune it through simulations. |
| D-29 | Event costs | Confirmed | Events may have an automatic cost on encounter and a separate cost for a chosen response, in addition to percentage-based outcome rolls. Both are expense entries charged once. |
| D-30 | Career score inputs | Confirmed | One final result uses only final channel viewers and net profit earned during the run, weighted equally at 50% each. Net profit is sponsor + donation + subscription income minus all expenses; the pinned starting balance (initially 1,500 PLN) does not count. Drama has no direct score term. |
| D-31 | Score normalization | Confirmed | Use the uncapped square-root formula in `balance.md`, with first configuration values of 1,000 reference viewers, 5,000 PLN reference profit, and 500 points per component. Pin data and algorithm versions separately; any future tuning creates a new version. |
| D-32 | First event examples | Confirmed | The first documentation pass has 20 complete draft examples in `sample-events.md`; they satisfy the earlier request for a representative sample but are not approved SQLite content. |
| D-33 | Configurable parameters | Confirmed | Store run length and other tunable numeric values in one typed `GameParameters` row per published SQLite catalogue version. An active run pins its version. The initial field list and validation are in `event-system.md`; gameplay formulas remain in Domain code. |
| D-34 | Streamer name generator | Confirmed | Suggest a streamer name by concatenating two parts from the versioned SQLite vocabulary. The player may replace it with a custom name. Neither the chosen name nor any player record enters SQLite. Initial word lists and name-validation details in `streamer-name-generation.md` are proposed. |
| D-35 | Weekly-action catalogue | Proposed | Put weekly-action definitions, costs, and weighted outcomes in the same versioned SQLite catalogue so their tuning does not require a code deployment. Four provisional actions are documented; their final set and balance remain open. |
| D-36 | Logging and statistics | Confirmed | Record errors and operational information; measure app opens, where careers end, and the most common player choices. A page open is not a unique person, and abandonment can only be estimated without server-side run saves. |
| D-37 | Telemetry implementation | Proposed | Use JSON console logs and built-in .NET operational metrics; store only first-party aggregate gameplay counters in a separate `analytics.db` SQLite file. No external tracker or public analytics dashboard. The retention, privacy review, and operator reports are in `observability-and-analytics.md`. |
| D-38 | In-game currency | Confirmed | Use PLN. Display amounts consistently as PLN in both language versions; the initial bankruptcy threshold is -1,000 PLN. |
| D-39 | Setup scope | Confirmed | Ask for only a streamer name at the start. Do not add a separate channel name, content theme, or starting archetype in the first version. |
| D-40 | Platform choice | Confirmed | Do not ask the player to choose a streaming platform in the first version. Fictional platform references can remain in event flavor without a setup choice. |
| D-41 | Default locale | Confirmed | Start in Polish (`pl-PL`) with a visible switch to English (`en`); switching language never changes game state or dice rolls. |
| D-42 | Satirical allusions | Confirmed | Use sharp satire. Real personal names remain unknown in game text, while scenarios may be unmistakably reminiscent of particular Polish internet stories about public creators. Review each allusion before publication, including whether it identifies a private person or implies an unverified allegation. |
| D-43 | Event authoring | Confirmed | Provide an authenticated admin panel in the first version for drafting, reviewing, previewing, and publishing bilingual game content. Only the owner may access and publish content in the first version. |
| D-44 | Admin authentication | Confirmed | Sign in through GitHub OAuth. Allow only the owner's configured GitHub account; authorize by the stable GitHub user ID rather than the mutable login name. No player login is introduced. |
| D-45 | Admin origin | Confirmed | Host the panel at `admin.polskistreamersymulator.pl`, separate from the public game's `polskistreamersymulator.pl` origin. Keep all draft and publishing APIs protected server-side. |
| D-46 | Admin publishing roles | Confirmed | One owner account can draft, edit, preview, and publish. No separate editor role is needed in the first version. |
| D-47 | Additional topic exclusions | Confirmed | Do not add a blanket list of excluded topics beyond the existing content boundaries. Review sensitive public-creator allusions case by case before publication; private people must not be recognizable subjects. |
| D-48 | Aggregate telemetry default | Confirmed | Collect the specified first-party aggregate counts by default with a clear player notice and without a separate in-game opt-in step. Keep player names, saves, persistent identifiers, and third-party trackers out of analytics. Review applicable privacy and consent requirements before production collection. |
| D-49 | Telemetry retention | Confirmed | Keep daily aggregate gameplay counters for 12 months and operational node logs for 14 days, then delete them under an enforced retention job. Encrypted backup copies have a separate confirmed 30-day retention. |
| D-50 | Off-VPS backup destination and schedule | Confirmed | Send encrypted backups to the creator's own second machine. Back up daily and take a consistent backup before each catalogue publication or schema migration. Keep backup copies for 30 days; transfer and recovery details remain to be configured before release. |
| D-51 | Viewer milestones | Confirmed | No separate one-time audience milestones in the first version. Viewer thresholds can still make ordinary events eligible. |
| D-52 | Event chains | Confirmed | An outcome may unlock a later event through browser-local narrative state. The follow-up remains subject to its own encounter chance and may never appear. |
| D-53 | Event frequency | Confirmed | Aim for a selected event in most non-terminal weeks across representative 52-week runs. This is a balance target, not a guaranteed weekly event. |
| D-54 | Event repetition | Confirmed | Set repeat policy per event: some appear at most once per career; others may return after their authored cooldown. |
| D-55 | Events per week | Confirmed | A week can contain up to three events after its one weekly action. `MaxEventsPerWeek = 3` is a typed parameter in the first published SQLite catalogue; an active run pins it. Rolling each event at most once per week is the proposed deterministic selection rule. |
| D-56 | Debt-funded choices | Confirmed | A paid weekly action or event response may be selected without cash on hand even when its guaranteed cost alone temporarily reaches or crosses the pinned bankruptcy limit. Resolve its rolled outcome and the rest of that atomic step, then test the reconciled balance; an outcome can rescue the run. Automatic encounter costs retain their separate immediate bankruptcy check. |
| D-57 | Follow-up priority | Confirmed | A flagged story follow-up has the same selection priority as an ordinary event when both pass their occurrence rolls. It is not guaranteed and receives no priority boost. |
| D-58 | Save export/import | Confirmed | Include local file export and import of browser career saves in the first version. Validate imported state and confirm before overwriting a slot; no server-side player save is added. |
| D-59 | Additional early endings | Confirmed | The first version includes permanent ban (`permanent_ban`) and channel closure (`channel_closed`) as defeats, and retirement from streaming (`retired`) as a neutral ending, on rolled event outcomes. If the same response also reaches bankruptcy, the recap uses bankruptcy as the primary ending reason. |

## Questions to resolve before or during implementation

1. After playtests, what changes are needed to the costs and outcome distributions of the four accepted working actions in `balance.md`?
2. Where will the free, ad-free game be published beyond the confirmed website, if anywhere? This can be decided later.
3. If a platform choice is considered after the first version, what gameplay purpose would it serve? No platform choice is planned for the first version.
4. How will the confirmed 30-day encrypted backup transfer, monitoring, and restore credentials be configured on the second machine before release?
5. Does the proposed one-PLN-per-ten-viewers weekly subscription payout keep money meaningful alongside sponsor, donation, and expense ranges over 52 weeks?
6. During playtests, does the confirmed initial score scale give audience and net profit comparable influence across representative 52-week careers? If not, prepare a new version rather than silently changing active runs.
7. Review the privacy notice and applicable consent requirements before enabling the confirmed default aggregate telemetry in production.
8. Verify the proposed two-file SQLite layout, persistent-volume mount, backup procedure, and VPS disk headroom before production deployment.
9. Review Polish and English event translations before publication; both are required for the first version.
10. Set the first-release event count after scope and content-production estimates; 200–400 is under consideration, not yet a commitment.

## Decision procedure

When the creator answers a question, update its row and the authoritative document in the same change. If a playtest overturns a proposed default, record the new decision and why. Keep old rationale in version control rather than leaving contradictory rules in multiple documents.
