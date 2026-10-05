# Documentation index

## Status

This is a **pre-production baseline** updated 2026-10-05. It specifies an implementable first version, while marking choices that still need the creator's confirmation. `PolskiStreamerSymulatorApp/` contains a .NET 10 / Blazor WebAssembly skeleton, but no game behavior or server project yet.

| Document | Authoritative for | Status |
| --- | --- | --- |
| [Product brief](product-brief.md) | Product promise, audience, commercial stance, scope, success criteria | Proposed baseline |
| [Game design](game-design.md) | Confirmed three-statistic, dice-driven core, configurable run length, bankruptcy ending, player name choice, and final-score inputs; proposed weekly menu | Confirmed core; weekly choices pending |
| [Balance](balance.md) | First published numeric defaults, configurable bankruptcy and duration, final-score formula, provisional income, odds, and tuning method | Initial values confirmed; normalization and income proposed |
| [Event catalogue](event-catalogue.md) | Authoring contract, design example, and staged target of at least 200 events | Content plan; final Polish copy and odds pending |
| [Sample events](sample-events.md) | Twenty draft events with eligibility, costs, Polish choices, odds, and effects | Examples only; not published or balance-approved |
| [Streamer name generation](streamer-name-generation.md) | Two-part SQL Server vocabulary, suggestion flow, custom entry, validation, and examples | Generator direction confirmed; seed words and input bounds proposed |
| [Experience and content](experience-and-content.md) | Screens, feedback, writing, accessibility, content boundaries | Proposed baseline |
| [Technical design](technical-design.md) | .NET stack, CQRS project graph, SQL Server proposal, server, browser saves | Architecture baseline; database switch proposed |
| [Logging and analytics](observability-and-analytics.md) | Server and client errors, operational metrics, aggregate gameplay counters, data handling | Proposed design; not implemented |
| [Deployment](deployment.md) | VPS, k3s, Ansible, HTTPS, GitHub Actions, operations and backups | Infrastructure scaffold; not deployed |
| [Event system](event-system.md) | SQL Server game-catalogue schema for weekly actions, events, parameters, and name parts; chance rolls, publishing, version pinning | Proposed detailed design |
| [Testing strategy](testing-strategy.md) | Proposed test projects, scenarios, tools, and gates | Proposed baseline |
| [Library guide](library-guide.md) | Wolverine, Mapster, FluentValidation, persistence, and package timing | Proposed baseline |
| [Delivery plan](delivery-plan.md) | Phases, dependencies, definition of done | Proposed baseline |
| [Decisions](decisions.md) | Decision state and unresolved product questions | Living log |
| [Reference analysis](reference-analysis.md) | Verified reference observations and design separation | Research note |

## Reading order

1. Read the product brief for the intended game and scope.
2. Read game design, balance, the event catalogue, and streamer name generation to understand rules and setup.
3. Read experience, technical design, logging and analytics, testing strategy, library guide, and deployment before building a feature.
4. Use the delivery plan to choose the next slice and decisions to check unresolved choices.

## Change policy

Each behavior should have one home: rules in game design, numeric constants in balance, presentation in experience and content, technical contracts in technical design. Cross-link instead of copying entire sections. Record new decisions and their rationale in the decision log. Move a proposal to **confirmed** only after the creator approves it or implementation makes it an explicit agreed project choice.
