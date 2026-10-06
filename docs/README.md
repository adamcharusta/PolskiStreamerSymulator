# Documentation index

## Status

This is a **pre-production baseline** updated 2026-10-06. It specifies an implementable first version, while marking choices that still need the creator's confirmation. `PolskiStreamerSymulatorApp/` contains an ASP.NET Core server that hosts the Blazor WebAssembly client and health endpoints, but no game behavior yet.

| Document | Authoritative for | Status |
| --- | --- | --- |
| [Product brief](product-brief.md) | Product promise, audience, commercial stance, scope, success criteria | Free/ad-free and bilingual first release confirmed; remaining scope proposed |
| [Game design](game-design.md) | Three-statistic dice-driven career, up to three events per week, debt-funded choices, story chains, endings, and weekly menu | Core rules and special ending classifications confirmed |
| [Balance](balance.md) | First published numeric defaults, configurable bankruptcy and duration, final-score formula, provisional income, odds, and tuning method | Initial values and score formula confirmed; action odds and subscription income to test |
| [Event catalogue](event-catalogue.md) | Bilingual authoring contract, design example, and proposed 200–400 event range for first release | Content plan; exact count, reviewed Polish/English copy, and final odds pending |
| [Sample events](sample-events.md) | Twenty Polish draft events with eligibility, costs, choices, odds, effects, and an optional story chain | Examples only; English adaptation and balance review pending |
| [Streamer name generation](streamer-name-generation.md) | Two-part SQLite vocabulary, suggestion flow, custom entry, validation, and examples | Generator direction confirmed; seed words and input bounds proposed |
| [Experience and content](experience-and-content.md) | Screens, feedback, bilingual writing, accessibility, content boundaries | Language and setup scope confirmed; detailed presentation proposed |
| [Technical design](technical-design.md) | .NET stack, CQRS project graph, SQLite with EF Core, server, browser saves and file export/import | Architecture baseline; SQLite choice confirmed, file layout proposed |
| [Logging and analytics](observability-and-analytics.md) | Server and client errors, operational metrics, aggregate gameplay counters, data handling | Proposed design; not implemented |
| [Deployment](deployment.md) | VPS, k3s, Ansible, HTTPS, GitHub Actions, operations and backups | Infrastructure scaffold; not deployed |
| [Event system](event-system.md) | SQLite catalogue schema for actions, events, narrative flags, repeat policy, parameters, and names; rolls, publishing, version pinning | Proposed detailed design |
| [Testing strategy](testing-strategy.md) | Test projects, test platform, scenarios, tools, and gates | Baseline; Server integration tests run on Microsoft Testing Platform |
| [Library guide](library-guide.md) | Confirmed Wolverine, Mapster, and FluentValidation; persistence and package timing | Stack decisions confirmed; package timing planned |
| [Delivery plan](delivery-plan.md) | Phases, ordered work packages, dependencies, acceptance evidence, and release gates | Actionable plan; dates and estimates intentionally unset |
| [Decisions](decisions.md) | Decision state and unresolved product questions | Living log |
| [Reference analysis](reference-analysis.md) | Verified reference observations and design separation | Research note |

## Reading order

1. Read the product brief for the intended game and scope.
2. Read game design, balance, the event catalogue, and streamer name generation to understand rules and setup.
3. Read experience, technical design, logging and analytics, testing strategy, library guide, and deployment before building a feature.
4. Use the delivery plan to choose the next slice and decisions to check unresolved choices.

## Change policy

Each behavior should have one home: rules in game design, numeric constants in balance, presentation in experience and content, technical contracts in technical design. Cross-link instead of copying entire sections. Record new decisions and their rationale in the decision log. Move a proposal to **confirmed** only after the creator approves it or implementation makes it an explicit agreed project choice.

Work-package design specs and implementation plans live in `docs/superpowers/specs/` and `docs/superpowers/plans/`. They record how a package was built; the documents above stay authoritative for rules and contracts.
