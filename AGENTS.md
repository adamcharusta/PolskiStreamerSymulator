# Agent guide

This file is a navigation map for contributors and coding agents. The authoritative specifications live in `docs/`; do not duplicate their rules here.

| Need | Read |
| --- | --- |
| Documentation status, ownership, and reading order | `docs/README.md` |
| Product goal, audience, scope, business model, success criteria | `docs/product-brief.md` |
| Player journey, turn sequence, mechanics, ending, and MVP rules | `docs/game-design.md` |
| Starting numbers, formulas, event weights, and balance targets | `docs/balance.md` |
| Event eligibility, choices, and effects | `docs/event-catalogue.md` |
| SQLite event schema, dice rolls, versioning, and authoring | `docs/event-system.md` |
| Screens, Polish copy conventions, accessibility, and content boundaries | `docs/experience-and-content.md` |
| Stack, architecture, state schema, saves, and quality gates | `docs/technical-design.md` |
| VPS, k3s, Ansible, GitHub Actions, TLS, backups, and rollout | `docs/deployment.md` |
| Test project layout, test cases, and tools | `docs/testing-strategy.md` |
| Optional .NET libraries and when to add them | `docs/library-guide.md` |
| Delivery phases, acceptance criteria, and dependencies | `docs/delivery-plan.md` |
| Confirmed choices, assumptions awaiting validation, and unresolved questions | `docs/decisions.md` |
| What was observed in the reference game and what is original here | `docs/reference-analysis.md` |

## Working agreement

- Read `docs/decisions.md` before implementing a proposed choice. Ask for a decision when it blocks a meaningful product choice; otherwise implement the documented default and record the assumption.
- Keep game mechanics in `docs/game-design.md`, numeric tuning in `docs/balance.md`, and implementation details in `docs/technical-design.md`.
- Update the relevant document when changing behavior. Keep `CLAUDE.md` and this file as indexes.
- Keep documentation and code comments in English. Write player-facing text in Polish and keep it outside simulation logic.
- Treat the linked football simulator as inspiration for the career loop only. Create original writing, visuals, event data, and code.
- Do not claim a feature is implemented because it appears in the specification. `PolskiStreamerSymulatorApp/` currently contains only a .NET/Blazor skeleton.
