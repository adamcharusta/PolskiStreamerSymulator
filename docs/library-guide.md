# Library choices for the .NET solution

This is a selection guide, not a dependency-install checklist. The solution currently includes only the packages listed in `Directory.Packages.props` and has no game behavior yet. Add a library when a feature needs it, pin its version centrally, build, and record any architectural effect.

| Library | Recommendation | Scope and reason |
| --- | --- | --- |
| [WolverineFX](https://wolverinefx.net/tutorials/mediator.html) | Confirmed; adopt on the server with the first command handler | CQRS dispatch and handler conventions; mediator-only mode for the MVP |
| [Mapster](https://github.com/MapsterMapper/Mapster) | Confirmed; add with the first API DTO mapping | Boundary mapping in `Server`; do not replace domain methods with mappings |
| [FluentValidation](https://docs.fluentvalidation.net/en/latest/aspnet.html) plus [Wolverine integration](https://wolverinefx.net/guide/handlers/fluent-validation) | Confirmed; add with the first command validator | Input rules in `Application`, called from the Wolverine pipeline or explicitly; domain invariants still mandatory |
| [EF Core SQLite provider](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/) (`Microsoft.EntityFrameworkCore.Sqlite`) | Add with the first catalogue repository | Two DbContexts for proposed `catalog.db` and `analytics.db` files in `Infrastructure`; no player records |
| [System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview) | Use built-in | API and versioned save serialization through the source-generated `ContractsJsonContext`; avoid another JSON package without a concrete gap |
| `ILogger<T>`, health checks, Problem Details; `Microsoft.AspNetCore.OpenApi` when an OpenAPI document is needed | Prefer Microsoft ASP.NET Core facilities | Diagnostics and HTTP contracts before adding third-party logging or API packages |
| [bUnit](https://bunit.dev/docs/getting-started/) and [Playwright](https://playwright.dev/dotnet/docs/intro) | Add with UI tests | Component-level and browser-level confidence |
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | Adopted with the Server in F1 | `UseWebAssemblyDebugging` for client debugging in Development; `MapStaticAssets` serves the client files, so `UseBlazorFrameworkFiles` is not used |

## Packages to defer

- `WolverineFx.EntityFrameworkCore`, durable transports, outbox, and queues: useful when a real asynchronous workflow or transactional message publication appears; ordinary synchronous commands do not justify them.
- Serilog or another logging provider: built-in structured logging is enough until deployment needs a specific sink.
- Redis/cache packages: a single-player MVP has no demonstrated cache bottleneck.
- Player identity packages: browser-local careers need no player accounts. The confirmed owner-only panel uses GitHub OAuth and a host-only admin session; use ASP.NET Core authentication/authorization and add the GitHub OAuth handler needed by the implementation. Keep the client secret and owner ID server-side.
- `FluentValidation.AspNetCore`: its automatic MVC validation is not a fit for a Blazor/Minimal API flow and is not recommended for new projects by its maintainers.
- Another mediator library: Wolverine already fills that role once selected.

## Existing package cleanup

`Application` and `Infrastructure` reference `Microsoft.Extensions.DependencyInjection.Abstractions` for their registration extensions instead of the WebAssembly package, and the WebAssembly client references only `Contracts`. `Ardalis.GuardClauses` is still present in both projects; retain it only where it improves boundary checks, and keep domain invariants explicit in domain types.
