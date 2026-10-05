# Documentation index

## Status

This is a **pre-production baseline** dated 2026-10-04. It specifies an implementable first version, while marking choices that still need the creator's confirmation. `PolskiStreamerSymulatorApp/` contains a .NET 10 / Blazor WebAssembly skeleton, but no game behavior or server project yet.

| Document | Authoritative for | Status |
| --- | --- | --- |
| [Product brief](product-brief.md) | Product promise, audience, commercial stance, scope, success criteria | Proposed baseline |
| [Game design](game-design.md) | Rules, state, weekly loop, progression, failure and endings | Proposed baseline |
| [Balance](balance.md) | Initial values, calculations, randomness, simulation targets | Tuning hypothesis |
| [Event catalogue](event-catalogue.md) | MVP event eligibility, choices, and numeric effects | Rules baseline; Polish copy pending |
| [Experience and content](experience-and-content.md) | Screens, feedback, writing, accessibility, content boundaries | Proposed baseline |
| [Technical design](technical-design.md) | .NET stack, CQRS project graph, server, browser saves | Architecture baseline |
| [Deployment](deployment.md) | VPS, k3s, Ansible, HTTPS, GitHub Actions, operations and backups | Infrastructure scaffold; not deployed |
| [Event system](event-system.md) | SQLite catalogue schema, chance rolls, content publishing, version pinning | Proposed detailed design |
| [Testing strategy](testing-strategy.md) | Proposed test projects, scenarios, tools, and gates | Proposed baseline |
| [Library guide](library-guide.md) | Wolverine, Mapster, FluentValidation, persistence, and package timing | Proposed baseline |
| [Delivery plan](delivery-plan.md) | Phases, dependencies, definition of done | Proposed baseline |
| [Decisions](decisions.md) | Decision state and unresolved product questions | Living log |
| [Reference analysis](reference-analysis.md) | Verified reference observations and design separation | Research note |

## Reading order

1. Read the product brief for the intended game and scope.
2. Read game design, balance, and the event catalogue to understand rules and initial numbers.
3. Read experience, technical design, testing strategy, library guide, and deployment before building a feature.
4. Use the delivery plan to choose the next slice and decisions to check unresolved choices.

## Change policy

Each behavior should have one home: rules in game design, numeric constants in balance, presentation in experience and content, technical contracts in technical design. Cross-link instead of copying entire sections. Record new decisions and their rationale in the decision log. Move a proposal to **confirmed** only after the creator approves it or implementation makes it an explicit agreed project choice.
