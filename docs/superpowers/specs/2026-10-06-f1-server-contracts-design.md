# F1 design: Server and Contracts projects

| Field | Value |
| --- | --- |
| Work package | F1 in the [delivery plan](../../delivery-plan.md) |
| Date | 2026-10-06 |
| Status | Proposed, awaiting the creator's review |
| Authoritative documents | [Technical design](../../technical-design.md), [deployment](../../deployment.md), [testing strategy](../../testing-strategy.md), [library guide](../../library-guide.md) |

This spec records how F1 will be built. The documents above stay authoritative for rules and contracts; this file does not replace them.

## Goal

Turn the template skeleton into the confirmed host-and-client shape, so later packages (F2 contracts and the S-series gameplay work) have a correct place to live. F1 is done when:

1. The solution builds with warnings treated as errors.
2. BlazorApp references only Contracts among the solution's projects.
3. The Server serves the Blazor WebAssembly client and its health endpoints.

## Inputs

Stated by the creator and recorded as confirmed in the documentation:

- D-05: .NET 10, standalone Blazor WebAssembly, central package management, and the Domain/Application/Infrastructure/BlazorApp split.
- D-13 and D-14: CQRS layering and an ASP.NET Core server. The target graph in the technical design adds Contracts and Server.
- The deployment design expects `PolskiStreamerSymulatorApp/src/Server/Server.csproj`, HTTP port 8080, `/health/live` and `/health/ready`, and one replica.

Assumptions made for this design. Each one is reversible:

- The new projects keep the `PolskiStreamerSymulatorApp.<Project>` assembly naming convention, and the Dockerfile entrypoint is corrected to match it.
- Both health paths are implemented now. The Kubernetes probes already call both, and without explicit endpoints the client fallback would answer an unmapped probe path with the client page and status 200.
- The Server integration tests are written first, following test-driven development. This pulls the health test from F3 into F1; F3 keeps the CI gate.
- Tests use the xUnit v3 package line 4 in VSTest mode (`xunit.v3.mtp-off`), explained under Testing.

## Non-goals

Wolverine, Mapster, FluentValidation, EF Core and SQLite, JSON console logging, centralized error handling and Problem Details (S5), admin host separation, contract DTOs (F2), the CI workflow (F3), removal of the template pages (Counter, Weather), and Kubernetes manifest changes.

## Approaches considered

1. **The Server hosts the standalone client's build output (recommended).** The Server references BlazorApp. The client's static web assets flow into the Server, which serves them with `MapStaticAssets` and an `index.html` fallback. This matches D-05 and the documented graph, keeps a single container, and leaves the client a standalone app with its own `index.html` and browser-local state. Microsoft's .NET 10 documentation no longer has a "hosted WebAssembly" template. Its [static files guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/static-files?view=aspnetcore-10.0) states that `MapStaticAssets` replaces `UseBlazorFrameworkFiles` in apps that serve WebAssembly framework files, and that the WebAssembly SDK configures static web assets for a server that consumes a standalone project's outputs.
2. **Convert to a Blazor Web App with Interactive WebAssembly rendering.** This is Microsoft's current shape for server-hosted WebAssembly. It moves the root component and render-mode configuration into the server, raises prerendering concerns for browser-only saves, and contradicts the standalone client in D-05.
3. **Host the client separately** in an nginx container or as Traefik static files, with an API-only server. This adds a second image plus CORS or path routing, and contradicts the single server pod in the deployment design.

## Design

### Project graph after F1

```text
Domain                        (no project references)
Application  ---------------> Domain
Infrastructure -------------> Application (Domain transitively)
Contracts                     (no project references; no types until F2)
Server ---------------------> Application, Infrastructure, Contracts, BlazorApp
BlazorApp ------------------> Contracts
tests/Server.IntegrationTests -> Server
```

### Changes per project

| Project | Change |
| --- | --- |
| `Application` | `AddApplicationServices(this IServiceCollection services)` returns `services`. The `Microsoft.AspNetCore.Components.WebAssembly` package is replaced by `Microsoft.Extensions.DependencyInjection.Abstractions`. |
| `Infrastructure` | The same change for `AddInfrastructureServices`, with the same package swap. |
| `Contracts` (new) | `src/Contracts/Contracts.csproj`: a plain class library named `PolskiStreamerSymulatorApp.Contracts`, with no references and no source files until F2. The empty project fixes the reference direction now. |
| `Server` (new) | `src/Server/Server.csproj` on `Microsoft.NET.Sdk.Web`, named `PolskiStreamerSymulatorApp.Server`, with the references from the graph and the `Microsoft.AspNetCore.Components.WebAssembly.Server` package. Files: `Program.cs`, `Health/HealthEndpoints.cs`, `appsettings.json`, `Properties/launchSettings.json`. |
| `BlazorApp` | The Infrastructure project reference is replaced by Contracts. `Program.cs` drops the two registration calls and their `using` directives. The standalone launch profile and the DevServer package stay for UI-only work. |
| Solution | Contracts and Server join the `src` folder; the test project joins the `tests` folder. |
| `deploy/Dockerfile` | The entrypoint becomes `PolskiStreamerSymulatorApp.Server.dll`, and the header comment no longer says the Server is missing. |

### Server composition

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.MapHealthEndpoints();
app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.Run();
```

- There is no `UseHttpsRedirection` or HSTS. TLS ends at Traefik, the pod listens on HTTP port 8080, and the HTTPS redirect already exists in `deploy/k8s/redirect-https.yaml`.
- There is no `UseBlazorFrameworkFiles`. `MapStaticAssets` serves the `_framework` assets with build-time content types and caching headers.
- `MapFallbackToFile("index.html")` keeps client routes such as `/counter` working after a reload. Explicit endpoints take precedence over the fallback. When the first API endpoint arrives in S4, unknown `/api` paths must return 404 instead of the client page; that rule is out of scope here because no API exists yet.

### Health endpoints

`HealthEndpoints.MapHealthEndpoints` maps two paths:

| Path | Checks run | Meaning |
| --- | --- | --- |
| `/health/live` | None | The process accepts HTTP requests. |
| `/health/ready` | Checks tagged `ready` | The dependencies needed to serve players are available. No such check exists until S2 adds the published-catalogue check. |

Both return status 200 with the default plain-text body `Healthy` while healthy, and status 503 when a check they run fails. Their semantics follow the [application readiness gates](../../deployment.md#application-readiness-gates).

### Packages

Central versions added to `Directory.Packages.props`. Each is the current stable release, checked on NuGet on 2026-10-05:

| Package | Version | Used by |
| --- | --- | --- |
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | 10.0.12 | Server |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.12 | Application, Infrastructure |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.12 | Server.IntegrationTests |
| `Microsoft.NET.Test.Sdk` | 18.10.1 | Server.IntegrationTests |
| `xunit.v3.mtp-off` | 4.0.1 | Server.IntegrationTests |
| `xunit.runner.visualstudio` | 4.0.0 | Server.IntegrationTests |

### Development entry point

`dotnet run --project src/Server` becomes the supported way to run the app. Its `http` and `https` launch profiles use ports 5180 and 7180 and include the WebAssembly debugging `inspectUri`. BlazorApp keeps its own profile on ports 5182 and 7149 for UI-only experiments; that mode cannot reach future server APIs.

## Testing

`tests/Server.IntegrationTests` uses `WebApplicationFactory<Program>` against the real Server in the Development environment. The tests are written before the Server exists and must fail first.

| Test | Expectation |
| --- | --- |
| Live and ready health | `GET /health/live` and `GET /health/ready` return 200 with the body `Healthy`. |
| Host page | `GET /` returns 200 `text/html` containing `<div id="app">` and a resolved `_framework/blazor.webassembly.<fingerprint>.js` script reference, with no `#[.{fingerprint}]` placeholder left. |
| Boot script | The script URL taken from the host page returns 200 with a JavaScript content type. |
| Client route fallback | `GET /counter` returns the host page. |

xUnit package choice: the testing strategy names xUnit. Its current package line, xunit.v3 4.x, defaults to Microsoft Testing Platform v2. The .NET 10 SDK runs those projects only when `global.json` opts `dotnet test` into that mode. The deploy workflow runs `dotnet test` from the repository root, where `PolskiStreamerSymulatorApp/global.json` is not found, so that opt-in would not apply there. The `xunit.v3.mtp-off` package, used with `xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk`, keeps the classic VSTest mode and works from any directory. Moving to Microsoft Testing Platform later is a separate change to the `global.json` location and the workflow command.

## Verification before completion

1. A solution build succeeds with zero warnings.
2. A solution test run passes from the repository root, the way the deploy workflow calls it.
3. `dotnet list src/BlazorApp/BlazorApp.csproj reference` shows only Contracts, and Application and Infrastructure no longer list the WebAssembly package.
4. `dotnet publish` of the Server in Release, then the published `PolskiStreamerSymulatorApp.Server.dll` run with `ASPNETCORE_ENVIRONMENT=Production` and `ASPNETCORE_HTTP_PORTS=8080`: the host page, the boot script, and both health paths respond as the tests expect.
5. If the local Docker daemon is running, `deploy/Dockerfile` builds and the container answers the same requests.

## Documentation updates

The implementation updates these documents in the same change:

- Technical design: the observed solution, the graph as implemented, an accurate hosting reference, and the DI registration.
- Library guide: the new packages and the finished package cleanup.
- Testing strategy: the first test project and the xUnit package choice.
- Deployment: the Server and health endpoints exist, and the Dockerfile entrypoint is corrected.
- Delivery plan: F1 is complete, and F3 still owes the CI gate.
- Decisions: D-14 wording now that Contracts exists.
- `README.md`, `AGENTS.md`, and the documentation index status, plus one index line explaining `docs/superpowers/`.

## Risks

- **Static web assets under the test host.** `MapStaticAssets` reads a build manifest next to the app assembly. If `WebApplicationFactory` does not find it, the host page test fails first, and the fix belongs in the test project setup rather than in production code.
- **The fallback serving the raw `index.html`.** If the fallback returned the unprocessed file, browsers would request `_framework/blazor.webassembly` without a fingerprint. The host page and boot script tests catch this in Development; verification step 4 catches it for published output.
- **xUnit 4 is a recent major version.** If it misbehaves, the fallback is `xunit.v3.mtp-off` 3.2.2 without test code changes.
