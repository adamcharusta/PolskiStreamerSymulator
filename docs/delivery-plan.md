# Delivery plan

This plan orders work; dates, estimates, and team size are not yet committed. Work package F1 is complete: the ASP.NET Core Server hosts the Blazor client and health endpoints. No game behavior has been implemented yet. The phases define outcomes; the work packages below make them actionable. A package is complete only when its stated evidence exists. Infrastructure preparation may run alongside game work, but production rollout waits for the release gates.

## Phase 0 — confirm the foundation

- Confirm the selected VPS/k3s deployment design and record what the creator's second machine needs for encrypted daily and pre-change backups with 30-day retention. Configure and rehearse transfer and recovery during release preparation.
- Record the integration requirements for GitHub OAuth owner login at `admin.polskistreamersymulator.pl`: a stable GitHub user ID allowlist, owner-only publication, and separation from browser-local player careers. Implement and test them with the admin panel.
- Done in F1: the `Server` and `Contracts` projects exist, the client no longer references Infrastructure, and DI composition lives in the server.
- Use the four accepted working weekly actions and test their first outcome distributions for money, viewers, and drama. The first published defaults of 52 weeks, three core statistics, and a -1,000 PLN bankruptcy ending are already confirmed; their numeric values will live in a versioned SQLite parameter row.
- Review the dice-driven weekly loop with one paper walkthrough and adjust choices that feel repetitive or have unclear risk.

**Exit:** all implementation-blocking decisions are recorded; documents agree on scope and terminology.

## Phase 1 — simulation vertical slice

- Extend the existing skeleton with one end-to-end CQRS request through the server.
- Implement setup without archetypes, including a two-part SQLite name suggestion and editable custom name; a pure engine, seeded randomness, one weekly action with a percentage-based outcome, and a report.
- Implement money, viewers, drama, weighted outcomes, categorized cash-flow entries, the proposed audience-based weekly subscription settlement, typed pinned game parameters and terminal loss state, and reason codes with one draft weekly-action definition plus a small representative event fixture drawn from `sample-events.md`.
- Add structured JSON server logs, centralized error handling, and built-in request/error metrics without logging player names or browser saves.
- Build a minimal UI with a mobile layout and visible decision costs. Keep text outside simulation logic and establish Polish/English resource keys early.

**Exit:** a player can start a run, choose a weekly action, see its rolled consequence, respond to an event, and understand the result; the four cash categories reconcile to closing money, and identical seed and choices reproduce the same report.

## Phase 2 — full first-year MVP

- Add progression to the pinned final week (initially 52), up to three sequential SQLite events in a week under the first pinned cap, money/viewer/drama gates across all bands, per-event once/cooldown rules, optional flag-gated follow-ups with equal selection priority, history, bankruptcy and the three approved special early ending reasons, and configured-completion recaps with one audience/net-profit career score. Support debt-funded choices whose rolled outcome can rescue a temporary threshold crossing; bankruptcy takes precedence if the same result also names a special ending. There are no separate viewer milestones. Review the 20 documented Polish draft events and prepare English adaptations before publishing any of them; set the first playable published count from repetition tests. The creator is considering roughly 200–400 events for the public release, with the final number still open.
- Build the authenticated admin panel for editing, reviewing, previewing, validating, and publishing weekly actions, events, one typed parameter row, and name parts. Restrict all admin endpoints on the server, audit operator actions, and keep old published catalogue versions available for browser saves. A source-controlled seed importer may bootstrap the first draft.
- Add the separate aggregate analytics SQLite database and first-party app-open/client-error endpoints. Count successful choices and endings, provide a private report, and keep analytics failures from blocking gameplay.
- Add the confirmed three browser-local manual save slots plus autosave, schema validation, and local JSON file export/import with explicit overwrite confirmation.
- Complete Polish and English UI resources and catalogue text, with a language switch that leaves the pinned run and dice sequence unchanged.
- Complete keyboard and narrow-viewport paths.
- Run automated balance sweeps against `balance.md`. Tune provisional action odds and effects; if the confirmed first score scale proves unbalanced, propose a versioned adjustment for future runs.

**Exit:** a full first-year career is playable and recoverable after reload, with no known dead ends or irreversible save corruption.

## Phase 3 — content and release readiness

- Polish edit and English adaptation of all player-facing copy for clarity, consistent risks, and satirical tone.
- Publish at least one reviewed bilingual outcome for each approved special ending reason: permanent ban and channel closure as defeats, and retirement as neutral.
- Review sharp, recognizable allusions about public creators for originality and accidental identification of private people. Apply the existing content boundaries and review sensitive themes case by case. Finish the agreed first-release event count through the admin publishing workflow.
- Human playtests across calm, middle, and high-drama paths; record unclear risks, dominant strategies, chain follow-up frequency, event repetitions, and whether events appear in most non-terminal weeks.
- Verify production build, versioned save behavior, accessibility checks, and mobile performance.
- Review Polish and English privacy notices and applicable consent requirements, enforce the confirmed 12-month aggregate-counter and 14-day node-log retention, rehearse daily and pre-change backup/restore to the second machine, and confirm the absence of a public statistics endpoint.
- Prepare a short release page explaining where saves live and how players can provide feedback.

**Exit:** the MVP success criteria in `product-brief.md` are met and the creator accepts the release build.

## Ordered work packages

The package IDs express the recommended implementation order. Dependencies refer to those IDs; packages without a direct dependency may proceed in parallel. Each package includes documentation updates for any rule or technical contract it changes. Implementation starts only after its relevant decision in [decisions](decisions.md) is confirmed or its documented default is accepted for the slice.

| ID | Deliverable | Depends on | Acceptance evidence |
| --- | --- | --- | --- |
| F1 | Add `Server` and `Contracts`, restore the intended Domain/Application/Infrastructure/BlazorApp dependency direction, and establish server-side DI composition | Existing skeleton | Solution builds; BlazorApp has no Infrastructure reference; the Server can serve the client and one health endpoint |
| F2 | Define versioned run-state, catalogue, cash-ledger, terminal-reason, RNG, and request/response contracts | F1 | Contracts cover pending and terminal states, pinned versions, three statistics, and localized IDs; validation rejects malformed or impossible state |
| F3 | Add a CI build/test gate and the first Server integration test for the health endpoint; add other test projects alongside the behavior they verify | F1 | A clean checkout builds and runs the health test; the test-project layout follows `testing-strategy.md` without empty placeholder projects |
| S1 | Implement the pure weekly action engine and subscription settlement using typed pinned parameters | F2, F3 | The same seed and choice produce the same result; all four cash categories reconcile; the atomic bankruptcy check and debt rescue pass tests |
| S2 | Implement SQLite EF Core migrations, a source-controlled draft seed, immutable published catalogue versions, and catalogue validation | F2, F3 | A fresh database can be created and seeded; invalid odds, missing translations, invalid terminal classifications, and broken references block publication |
| S3 | Implement event eligibility, one encounter roll per eligible event per week, uniform selection, encounter/response costs, outcome rolls, and pending state | S1, S2 | One draft event can be encountered and resolved end to end; retries neither reroll nor double-charge; bankruptcy after an encounter cost stops before response selection |
| S4 | Connect CQRS handlers through WolverineFX and expose the first game flow from Server to BlazorApp | S1, S2, S3 | A player can name a streamer, choose an action, resolve one event, and see a report in the browser; the server stores no player career; unknown `/api` paths return 404 instead of the client host page |
| S5 | Add structured error logging, safe exception responses, and basic request/error metrics | F1, S4 | A failed request is traceable without logging player names, save bodies, or choice history |
| M1 | Extend the engine to the configured run length, up to three events per week, once/cooldown policies, drama/money/viewer gates, and chance-based follow-ups | S3 | Quiet, one-event, two-event, and three-event weeks are possible; no fourth event or same-week chain follow-up occurs; deterministic runs survive reload |
| M2 | Implement all endings and the final audience/net-profit score | M1 | Final-week completion, bankruptcy, ban, closure, and retirement recaps show the correct classification and one consistent score; bankruptcy has priority at a shared checkpoint |
| M3 | Add browser autosave, three manual slots, and validated JSON export/import | S4, M1 | Pending and finished careers round-trip; an incompatible or corrupt save remains untouched; import asks before overwriting |
| M4 | Build the owner-only admin panel for draft editing, preview, validation, and publication on the admin subdomain | S2, S4 | GitHub allowlisted owner access is enforced server-side; publication freezes a version and records an audit entry; prior versions remain readable by pinned saves |
| M5 | Complete Polish and English UI and catalogue text, responsive layout, and keyboard paths | S4, M1 | Language switching preserves IDs, odds, state, and RNG; every visible player flow works at 320 CSS pixels and with keyboard only |
| M6 | Add aggregate analytics and client-error reporting with retention jobs | S5, M2 | App opens, choices, and endings are counted without player identifiers or saves; analytics failures do not stop gameplay; retention is enforceable |
| R1 | Set the first-release event count, review and translate the catalogue, and publish at least one outcome for each special ending | M4, M5 | Every published event has reviewed Polish and English copy, valid odds, honest risk previews, and stable IDs; the agreed count is present |
| R2 | Run automated balance sweeps and human playtests across calm, mixed, and high-drama careers | M1, M2, R1 | Reports show event frequency, repetition, cash trajectory, score components, and dominant choices; tuning changes are versioned and documented |
| R3 | Prepare and rehearse the VPS/k3s/Ansible deployment, manual GitHub Actions rollout, TLS, encrypted second-machine backups, and restore | F1, M4 | A staging deployment and restore rehearsal pass; daily and pre-change backup jobs, monitoring, and 30-day backup retention are verified |
| R4 | Finish privacy and consent review, operational retention checks, accessibility/regression checks, and release sign-off | M3, M5, M6, R2, R3 | The product-brief acceptance criteria pass on the release build; the owner approves the release candidate before production rollout |

**Progress:** F1 was completed on 2026-10-06; its design spec and implementation plan are in `docs/superpowers/`. The health-endpoint integration test planned for F3 was written in F1 under test-driven development, so F3 added only the CI build and test gate. F2 was completed on 2026-10-07: Domain holds the run state, the PCG32 generator, the ledger, the pinned parameters, and the run-state validator, and Contracts holds the strict JSON wire format. F3 was completed on 2026-10-08: `.github/workflows/ci.yml` checks formatting, builds, tests, and starts the production container on every push (D-63).

The **first playable checkpoint** is F1–F3 and S1–S4. It uses a small bilingual event fixture and one complete browser loop, not the full release catalogue. The **full-game checkpoint** is M1–M6, with the 52-week career, all endings, local saves, admin publishing, language support, and aggregate telemetry. R1–R4 are release gates, not prerequisites for starting simulation work. Exact event count, balance tuning, backup transfer details, and privacy review remain tracked in [decisions](decisions.md); none blocks the first playable checkpoint.

## Backlog after the first release

Expand the reviewed event catalogue beyond the agreed first-release count as playtests show gaps. Evaluate multi-year play only after the 52-week balance and event variety hold up. Other candidates include more elaborate collaboration storylines, richer businesses, competing creators, shareable recaps, and offline installation. Each addition needs a product reason, save impact, and test plan.
