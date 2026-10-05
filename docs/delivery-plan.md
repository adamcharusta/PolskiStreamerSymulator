# Delivery plan

This plan orders work; dates and team size are not yet committed. A .NET 10 / Blazor solution skeleton exists, but no game behavior has been implemented.

## Phase 0 — confirm the foundation

- Follow the selected VPS/k3s deployment design and confirm the offsite backup destination before release.
- Add `Server` and `Contracts` projects; remove the client-to-Infrastructure reference and move DI composition to the server.
- Confirm the list of one-action-per-week options and their first outcome distributions for money, viewers, and drama. The first published defaults of 52 weeks, three core statistics, and a -1,000 PLN bankruptcy ending are already confirmed; their numeric values will live in a versioned SQL Server parameter row.
- Review the dice-driven weekly loop with one paper walkthrough and adjust choices that feel repetitive or have unclear risk.

**Exit:** all implementation-blocking decisions are recorded; documents agree on scope and terminology.

## Phase 1 — simulation vertical slice

- Extend the existing skeleton with one end-to-end CQRS request through the server.
- Implement setup without archetypes, including a two-part SQL Server name suggestion and editable custom name; a pure engine, seeded randomness, one weekly action with a percentage-based outcome, and a report.
- Implement money, viewers, drama, weighted outcomes, categorized cash-flow entries, the proposed audience-based weekly subscription settlement, typed pinned game parameters and terminal loss state, and reason codes with one draft weekly-action definition plus a small representative event fixture drawn from `sample-events.md`.
- Add structured JSON server logs, centralized error handling, and built-in request/error metrics without logging player names or browser saves.
- Build a minimal UI with a mobile layout and visible decision costs.

**Exit:** a player can start a run, choose a weekly action, see its rolled consequence, respond to an event, and understand the result; the four cash categories reconcile to closing money, and identical seed and choices reproduce the same report.

## Phase 2 — full first-year MVP

- Add progression to the pinned final week (initially 52), money/viewer/drama-gated SQL Server events across all drama bands, history, bankruptcy defeat and configured-completion recaps with one audience/net-profit career score. Review and import the 20 documented draft events, then set the first playable published count from repetition tests; the long-term target is at least 200.
- Add a validated import/publish path for weekly actions, events, one typed parameter row, and name parts; keep old published catalogue versions available for browser saves.
- Add the separate aggregate analytics SQL Server database and first-party app-open/client-error endpoints. Count successful choices and endings, provide a private report, and keep analytics failures from blocking gameplay.
- Add three browser-local save slots plus autosave and schema validation.
- Complete keyboard and narrow-viewport paths.
- Run automated balance sweeps and adjust odds, effects, and the provisional equal-weight score reference values against `balance.md`.

**Exit:** a full first-year career is playable and recoverable after reload, with no known dead ends or irreversible save corruption.

## Phase 3 — content and release readiness

- Polish edit of all player-facing copy for clarity, consistency, and satirical tone.
- Human playtests across calm, middle, and high-drama paths; record unclear risks and dominant strategies.
- Verify production build, versioned save behavior, accessibility checks, and mobile performance.
- Review the Polish privacy notice and telemetry retention, rehearse analytics backup/restore, and confirm node log rotation and the absence of a public statistics endpoint.
- Prepare a short release page explaining where saves live and how players can provide feedback.

**Exit:** the MVP success criteria in `product-brief.md` are met and the creator accepts the release build.

## Backlog after the first release

Expand the reviewed event catalogue toward the creator's target of at least 200 events after the rules and authoring pipeline are stable. Evaluate multi-year play only after the 52-week balance and event variety hold up. Other candidates include collaboration chains, richer businesses, competing creators, export/import, shareable recaps, and offline installation. Each addition needs a product reason, save impact, and test plan.
