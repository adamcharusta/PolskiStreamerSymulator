# Claude project guide

This is the same project map used by other coding agents. Read `AGENTS.md` first for the documentation table and working agreement.

The key specifications are:

- `docs/product-brief.md` — purpose, audience, scope, and product constraints.
- `docs/game-design.md` — player experience and gameplay rules.
- `docs/balance.md` — provisional numeric model and balancing method.
- `docs/event-catalogue.md` — event conditions, choices, and effects.
- `docs/sample-events.md` — twenty draft event examples and provisional outcomes.
- `docs/streamer-name-generation.md` — SQLite name vocabulary, suggestions, and custom entry.
- `docs/event-system.md` — SQLite weekly actions, events, story flags, repeat policies, typed game parameters, name parts, probabilities, and publishing rules.
- `docs/experience-and-content.md` — screens, tone, accessibility, and content rules.
- `docs/technical-design.md` — .NET 10, CQRS project graph, server design, and persistence boundary.
- `docs/observability-and-analytics.md` — logging, operational metrics, aggregate gameplay counters, and privacy safeguards.
- `docs/testing-strategy.md` — test projects, cases, and tools.
- `docs/library-guide.md` — package choices and adoption timing.
- `docs/delivery-plan.md` — implementation order and acceptance criteria.
- `docs/decisions.md` — decisions, defaults, and open questions.
- `docs/reference-analysis.md` — reference observations and originality boundary.

Deployment guidance is in `docs/deployment.md` (VPS, k3s, Ansible, GitHub Actions, HTTPS, and backups).

For a new task, consult `docs/README.md` to identify the authoritative document, then update that document alongside any eventual code change. Documentation is in English; player-facing game text is planned in Polish and English.
