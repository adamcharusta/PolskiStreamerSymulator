# Delivery plan

This plan orders work; dates and team size are not yet committed. A .NET 10 / Blazor solution skeleton exists, but no game behavior has been implemented.

## Phase 0 — confirm the foundation

- Follow the selected VPS/k3s deployment design and confirm the offsite backup destination before release.
- Add `Server` and `Contracts` projects; remove the client-to-Infrastructure reference and move DI composition to the server.
- Confirm the business model, first-run length, and whether the initial balance model is the desired style.
- Review the 52-week loop with one paper walkthrough and adjust decisions that feel repetitive.

**Exit:** all implementation-blocking decisions are recorded; documents agree on scope and terminology.

## Phase 1 — simulation vertical slice

- Extend the existing skeleton with one end-to-end CQRS request through the server.
- Implement setup, a pure engine, seeded randomness, one week of choices, and a report.
- Implement the metrics, formula order, and reason codes without the full event catalogue.
- Build a minimal UI with a mobile layout and visible decision costs.

**Exit:** a player can start a run, make a choice, resolve one week, and understand the result; identical seed and choices reproduce the same report.

## Phase 2 — full first-year MVP

- Add 52-week progression, milestones, at least 12 complete SQLite-backed event definitions, sponsor choices, history, early ending, and recap.
- Add a validated content import/publish path and keep old published catalogue versions available for browser saves.
- Add three browser-local save slots plus autosave and schema validation.
- Complete keyboard and narrow-viewport paths.
- Run automated balance sweeps and adjust constants against `balance.md`.

**Exit:** a full first-year career is playable and recoverable after reload, with no known dead ends or irreversible save corruption.

## Phase 3 — content and release readiness

- Polish edit of all player-facing copy for clarity, consistency, and satirical tone.
- Human playtests across archetypes and strategies; record unclear choices and dominant strategies.
- Verify production build, versioned save behavior, accessibility checks, and mobile performance.
- Prepare a short release page explaining where saves live and how players can provide feedback.

**Exit:** the MVP success criteria in `product-brief.md` are met and the creator accepts the release build.

## Backlog after the first release

Evaluate multi-year play first: the current 52-week balance and event variety must hold up before extending the calendar. Then consider collaboration chains, richer businesses, competing creators, export/import, shareable recaps, and offline installation. Each addition needs a product reason, save impact, and test plan.
