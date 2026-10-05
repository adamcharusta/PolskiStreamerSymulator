# F1 Server and Contracts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the ASP.NET Core Server and the Contracts project, restore the documented dependency direction, and make the Server host the Blazor WebAssembly client and its health endpoints.

**Architecture:** `Application` and `Infrastructure` expose `IServiceCollection` registration extensions that the new `Server` composition root calls. The Server references `BlazorApp`, serves its static web assets with `MapStaticAssets`, and falls back to `index.html` for client routes. `/health/live` runs no checks and `/health/ready` runs the checks tagged `ready`.

**Tech Stack:** .NET 10 (SDK pin `10.0.201`, `latestFeature`), ASP.NET Core 10.0.12, standalone Blazor WebAssembly, xUnit (`xunit.v3` 4.0.1) on Microsoft Testing Platform v2, `Microsoft.AspNetCore.Mvc.Testing`.

**Spec:** `docs/superpowers/specs/2026-10-06-f1-server-contracts-design.md`

## Global Constraints

- Shell commands run from `PolskiStreamerSymulatorApp/` unless a step starts with its own `cd`. That folder holds the solution and `global.json`; `dotnet test` reads the test runner setting only there or below.
- `Directory.Build.props` sets `net10.0`, nullable, implicit usings, and `TreatWarningsAsErrors`. Every build must end with 0 warnings and 0 errors.
- Central package management: versions live only in `Directory.Packages.props`, and `PackageReference` items carry no `Version`.
- New projects set `RootNamespace` and `AssemblyName` to `PolskiStreamerSymulatorApp.<Project>`.
- C# follows `.editorconfig`: file-scoped namespaces, explicit types instead of `var`, braces, and LF line endings (`.gitattributes` normalizes to LF).
- Exact package versions: `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.12, `Microsoft.AspNetCore.Components.WebAssembly.Server` 10.0.12, `Microsoft.AspNetCore.Mvc.Testing` 10.0.12, `xunit.v3` 4.0.1, `Microsoft.NET.Test.Sdk` 18.10.1, `xunit.runner.visualstudio` 4.0.0.
- Health paths are exactly `/health/live` and `/health/ready`, and the readiness tag is exactly `ready`.
- Documentation and code comments are in English. This plan changes no player-facing text.
- Work stays on branch `feature/f1-server-contracts`. Never push. Every commit message ends with the trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Create and edit files with a file-writing or editing tool, not shell heredocs. The shell in this environment turns `\\` into `\`, which corrupts C# strings and XML paths.
- `tests/Domain.Tests/`, `src/Server/Properties/`, and `src/Contracts/` exist as empty, untracked folders from an earlier session. Put the new files into them as described and otherwise leave `tests/Domain.Tests/` alone.

## Review Focus

1. A failing dependency check tagged `ready` must turn `/health/ready` into 503 while `/health/live` stays 200, so Kubernetes stops traffic without restarting the pod. Owned by Task 2, test `FailingReadyCheckFailsReadinessButNotLiveness`.
2. A failing check without the `ready` tag, such as the future analytics database check, must not take the server out of rotation. Owned by Task 2, test `FailingUntaggedCheckFailsNeitherEndpoint`.
3. Every stylesheet, icon, and script the host page references must load; one 404 silently drops styles or stops the client from starting. Owned by Task 3, test `EveryLocalAssetReferencedByHostPageIsServed`.
4. A request for a missing or outdated static file must return 404 rather than the HTML host page, which a browser would try to parse as JavaScript or CSS. Owned by Task 3, test `MissingStaticFileReturnsNotFound`.
5. Published Release output, which differs from build output, must start under the Dockerfile's entrypoint name and boot the client in a real browser. Owned by Task 4, published-output and browser checks.

## File Structure

| Path | Responsibility |
| --- | --- |
| `PolskiStreamerSymulatorApp/src/Contracts/Contracts.csproj` | Empty HTTP contracts library; F2 adds the DTOs |
| `PolskiStreamerSymulatorApp/src/Server/Server.csproj` | ASP.NET Core host project |
| `PolskiStreamerSymulatorApp/src/Server/Program.cs` | Composition root and request pipeline |
| `PolskiStreamerSymulatorApp/src/Server/Health/HealthEndpoints.cs` | Health endpoint mapping and the readiness tag |
| `PolskiStreamerSymulatorApp/src/Server/appsettings.json` | Logging defaults |
| `PolskiStreamerSymulatorApp/src/Server/Properties/launchSettings.json` | Development launch profiles |
| `PolskiStreamerSymulatorApp/tests/Server.IntegrationTests/Server.IntegrationTests.csproj` | Test project on xUnit and Microsoft Testing Platform |
| `PolskiStreamerSymulatorApp/tests/Server.IntegrationTests/HealthEndpointTests.cs` | Health endpoint behavior |
| `PolskiStreamerSymulatorApp/tests/Server.IntegrationTests/ClientHostingTests.cs` | Client hosting behavior |

Modified: `Directory.Packages.props`, `global.json`, `PolskiStreamerSymulatorApp.sln`, the `Application`, `Infrastructure`, and `BlazorApp` projects, `.github/workflows/deploy.yml`, `deploy/Dockerfile`, `.dockerignore`, and the documents listed in each task.

---

### Task 1: Restore the layer dependency direction

**Files:**
- Create: `PolskiStreamerSymulatorApp/src/Contracts/Contracts.csproj`
- Modify: `PolskiStreamerSymulatorApp/Directory.Packages.props`
- Modify: `PolskiStreamerSymulatorApp/src/Application/Application.csproj`, `PolskiStreamerSymulatorApp/src/Application/DependencyInjection.cs`
- Modify: `PolskiStreamerSymulatorApp/src/Infrastructure/Infrastructure.csproj`, `PolskiStreamerSymulatorApp/src/Infrastructure/DependencyInjection.cs`
- Modify: `PolskiStreamerSymulatorApp/src/BlazorApp/BlazorApp.csproj`, `PolskiStreamerSymulatorApp/src/BlazorApp/Program.cs`
- Modify: `PolskiStreamerSymulatorApp/PolskiStreamerSymulatorApp.sln`
- Modify: `docs/technical-design.md`, `docs/library-guide.md`

**Interfaces:**
- Consumes: nothing new.
- Produces: `public static IServiceCollection AddApplicationServices(this IServiceCollection services)` in namespace `PolskiStreamerSymulatorApp.Application`; `public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)` in namespace `PolskiStreamerSymulatorApp.Infrastructure`; project `src/Contracts/Contracts.csproj` with assembly `PolskiStreamerSymulatorApp.Contracts` and no types.

- [ ] **Step 1: Record the structural failure this task removes**

Run:

```bash
dotnet list src/BlazorApp/BlazorApp.csproj reference
dotnet list src/Application/Application.csproj package
```

Expected now: BlazorApp lists `..\Infrastructure\Infrastructure.csproj`, and Application lists `Microsoft.AspNetCore.Components.WebAssembly`.

- [ ] **Step 2: Add the DI abstractions package version**

In `Directory.Packages.props`, add this line directly after the `Microsoft.AspNetCore.Components.WebAssembly.DevServer` line:

```xml
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.12" />
```

- [ ] **Step 3: Swap the package in Application and Infrastructure**

In both `src/Application/Application.csproj` and `src/Infrastructure/Infrastructure.csproj`, replace

```xml
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly"/>
```

with

```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
```

Keep the indentation each file already uses and leave the `Ardalis.GuardClauses` reference in place.

- [ ] **Step 4: Rewrite the Application registration extension**

Replace the whole content of `src/Application/DependencyInjection.cs` with:

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace PolskiStreamerSymulatorApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
```

- [ ] **Step 5: Rewrite the Infrastructure registration extension**

Replace the whole content of `src/Infrastructure/DependencyInjection.cs` with:

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace PolskiStreamerSymulatorApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
```

- [ ] **Step 6: Create the Contracts project**

Create `src/Contracts/Contracts.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>PolskiStreamerSymulatorApp.Contracts</RootNamespace>
    <AssemblyName>PolskiStreamerSymulatorApp.Contracts</AssemblyName>
  </PropertyGroup>

</Project>
```

- [ ] **Step 7: Point BlazorApp at Contracts**

In `src/BlazorApp/BlazorApp.csproj`, replace

```xml
<ProjectReference Include="..\Infrastructure\Infrastructure.csproj"/>
```

with

```xml
<ProjectReference Include="..\Contracts\Contracts.csproj"/>
```

Replace the whole content of `src/BlazorApp/Program.cs` with:

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PolskiStreamerSymulatorApp.BlazorApp;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
```

- [ ] **Step 8: Add Contracts to the solution**

Run:

```bash
dotnet sln PolskiStreamerSymulatorApp.sln add src/Contracts/Contracts.csproj --solution-folder src
```

Expected: ``Project `src\Contracts\Contracts.csproj` added to the solution.``

- [ ] **Step 9: Build**

Run: `dotnet build PolskiStreamerSymulatorApp.sln`

Expected: `Build succeeded.` with `0 Warning(s)` and `0 Error(s)`.

- [ ] **Step 10: Re-run the structural check**

Run:

```bash
dotnet list src/BlazorApp/BlazorApp.csproj reference
dotnet list src/Application/Application.csproj package
dotnet list src/Infrastructure/Infrastructure.csproj package
```

Expected: BlazorApp lists only `..\Contracts\Contracts.csproj`. Application and Infrastructure list `Ardalis.GuardClauses` and `Microsoft.Extensions.DependencyInjection.Abstractions`, and neither lists `Microsoft.AspNetCore.Components.WebAssembly`.

- [ ] **Step 11: Update the documents for this change**

In `docs/technical-design.md`, replace

```text
`Infrastructure` registers its adapters; `Server` composes the full application. Refactor the current empty registration extensions in `Application` and `Infrastructure` to extend `IServiceCollection` rather than `WebAssemblyHostBuilder`, then remove their `Microsoft.AspNetCore.Components.WebAssembly` references. `BlazorApp` should reference only `Contracts` among the project libraries.
```

with

```text
`Application` and `Infrastructure` expose `IServiceCollection` registration extensions, `AddApplicationServices` and `AddInfrastructureServices`, and reference no Blazor packages. `Infrastructure` registers its adapters there, and the composition root calls both. `BlazorApp` references only `Contracts` among the project libraries.
```

In the same file, delete this sentence from the paragraph under the project graph:

```text
The current client-to-Infrastructure project reference must be removed when this graph is implemented.
```

In `docs/library-guide.md`, replace the paragraph under `## Existing package cleanup` with:

```text
`Application` and `Infrastructure` reference `Microsoft.Extensions.DependencyInjection.Abstractions` for their registration extensions instead of the WebAssembly package, and the WebAssembly client references only `Contracts`. `Ardalis.GuardClauses` is still present in both projects; retain it only where it improves boundary checks, and keep domain invariants explicit in domain types.
```

- [ ] **Step 12: Commit**

```bash
cd .. && git add PolskiStreamerSymulatorApp/Directory.Packages.props PolskiStreamerSymulatorApp/PolskiStreamerSymulatorApp.sln PolskiStreamerSymulatorApp/src docs/technical-design.md docs/library-guide.md && git commit -m "Restore layer dependency direction and add Contracts project" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Server host with health endpoints, test first

**Files:**
- Create: `PolskiStreamerSymulatorApp/src/Server/Server.csproj`, `PolskiStreamerSymulatorApp/src/Server/Program.cs`, `PolskiStreamerSymulatorApp/src/Server/appsettings.json`, `PolskiStreamerSymulatorApp/src/Server/Health/HealthEndpoints.cs`
- Create: `PolskiStreamerSymulatorApp/tests/Server.IntegrationTests/Server.IntegrationTests.csproj`, `PolskiStreamerSymulatorApp/tests/Server.IntegrationTests/HealthEndpointTests.cs`
- Modify: `PolskiStreamerSymulatorApp/Directory.Packages.props`, `PolskiStreamerSymulatorApp/global.json`, `PolskiStreamerSymulatorApp/PolskiStreamerSymulatorApp.sln`
- Modify: `.github/workflows/deploy.yml`, `docs/testing-strategy.md`, `docs/deployment.md`

**Interfaces:**
- Consumes: `AddApplicationServices` and `AddInfrastructureServices` from Task 1.
- Produces: `public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)` and `public const string ReadyTag = "ready"` in `internal static class PolskiStreamerSymulatorApp.Server.Health.HealthEndpoints`; the top-level `Program` class that `WebApplicationFactory<Program>` uses (the .NET 10 web SDK makes it public, so no manual declaration is needed); the test project with a global `using Xunit;`.

- [ ] **Step 1: Create the Server skeleton without endpoints**

Create `src/Server/Server.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <RootNamespace>PolskiStreamerSymulatorApp.Server</RootNamespace>
    <AssemblyName>PolskiStreamerSymulatorApp.Server</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Application\Application.csproj" />
    <ProjectReference Include="..\Contracts\Contracts.csproj" />
    <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

Create `src/Server/Program.cs`:

```csharp
using PolskiStreamerSymulatorApp.Application;
using PolskiStreamerSymulatorApp.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

WebApplication app = builder.Build();

app.Run();
```

Create `src/Server/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Run:

```bash
dotnet sln PolskiStreamerSymulatorApp.sln add src/Server/Server.csproj --solution-folder src
dotnet build PolskiStreamerSymulatorApp.sln
```

Expected: the project is added, and the build ends with `0 Warning(s)` and `0 Error(s)`.

- [ ] **Step 2: Add the test infrastructure**

In `Directory.Packages.props`, add this line directly after the `Microsoft.AspNetCore.Components.WebAssembly.DevServer` line:

```xml
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.12" />
```

Add these lines directly after the `Microsoft.Extensions.DependencyInjection.Abstractions` line:

```xml
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.10.1" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="4.0.0" />
    <PackageVersion Include="xunit.v3" Version="4.0.1" />
```

Replace the whole content of `global.json` with:

```json
{
  "sdk": {
    "version": "10.0.201",
    "rollForward": "latestFeature"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

Create `tests/Server.IntegrationTests/Server.IntegrationTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>PolskiStreamerSymulatorApp.Server.IntegrationTests</RootNamespace>
    <AssemblyName>PolskiStreamerSymulatorApp.Server.IntegrationTests</AssemblyName>
    <OutputType>Exe</OutputType>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="xunit.v3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Server\Server.csproj" />
  </ItemGroup>

</Project>
```

Run:

```bash
dotnet sln PolskiStreamerSymulatorApp.sln add tests/Server.IntegrationTests/Server.IntegrationTests.csproj --solution-folder tests
```

Expected: ``Project `tests\Server.IntegrationTests\Server.IntegrationTests.csproj` added to the solution.``

- [ ] **Step 3: Write the failing health tests**

Create `tests/Server.IntegrationTests/HealthEndpointTests.cs`:

```csharp
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PolskiStreamerSymulatorApp.Server.IntegrationTests;

public sealed class HealthEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpointReportsHealthyWhenNoCheckFails(string path)
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("/health/live", HttpStatusCode.OK)]
    [InlineData("/health/ready", HttpStatusCode.ServiceUnavailable)]
    public async Task FailingReadyCheckFailsReadinessButNotLiveness(string path, HttpStatusCode expectedStatus)
    {
        await using WebApplicationFactory<Program> failingFactory = WithFailingCheck(["ready"]);
        using HttpClient client = failingFactory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task FailingUntaggedCheckFailsNeitherEndpoint(string path)
    {
        await using WebApplicationFactory<Program> failingFactory = WithFailingCheck([]);
        using HttpClient client = failingFactory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private WebApplicationFactory<Program> WithFailingCheck(string[] tags)
    {
        return factory.WithWebHostBuilder(builder => builder.ConfigureServices(services => services
            .AddHealthChecks()
            .AddCheck("simulated-failure", () => HealthCheckResult.Unhealthy("Simulated dependency failure."), tags)));
    }
}
```

The literal `"ready"` is deliberate: it pins the tag contract that S2's catalogue check will rely on.

- [ ] **Step 4: Run the tests and see them fail**

Run: `dotnet test --project tests/Server.IntegrationTests/Server.IntegrationTests.csproj`

Expected: `failed: 6`, `succeeded: 0`. Each failure reports `Actual: NotFound`, because the skeleton maps no endpoints.

- [ ] **Step 5: Implement the health endpoints**

Create `src/Server/Health/HealthEndpoints.cs`:

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace PolskiStreamerSymulatorApp.Server.Health;

/// <summary>
/// Maps the liveness and readiness probes that Kubernetes calls.
/// </summary>
internal static class HealthEndpoints
{
    /// <summary>
    /// Tag for health checks that must pass before the server receives player traffic.
    /// </summary>
    public const string ReadyTag = "ready";

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = static _ => false,
        });

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = static registration => registration.Tags.Contains(ReadyTag),
        });

        return endpoints;
    }
}
```

Replace the whole content of `src/Server/Program.cs` with:

```csharp
using PolskiStreamerSymulatorApp.Application;
using PolskiStreamerSymulatorApp.Infrastructure;
using PolskiStreamerSymulatorApp.Server.Health;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

app.MapHealthEndpoints();

app.Run();
```

- [ ] **Step 6: Run the tests and see them pass**

Run: `dotnet test --project tests/Server.IntegrationTests/Server.IntegrationTests.csproj`

Expected: `Test run summary: Passed!` with `total: 6` and `failed: 0`.

- [ ] **Step 7: Run the deploy workflow's tests from the solution folder**

In `.github/workflows/deploy.yml`, replace

```yaml
      - name: Test solution
        run: dotnet test PolskiStreamerSymulatorApp/PolskiStreamerSymulatorApp.sln -c Release
```

with

```yaml
      - name: Test solution
        working-directory: PolskiStreamerSymulatorApp
        run: dotnet test --solution PolskiStreamerSymulatorApp.sln -c Release
```

Then run the same command locally: `dotnet test --solution PolskiStreamerSymulatorApp.sln -c Release`

Expected: `Passed!` with `total: 6` and `failed: 0`.

- [ ] **Step 8: Update the testing strategy**

In `docs/testing-strategy.md`, replace the first paragraph (it starts with "The current solution has no test projects.") with:

```text
The solution's first test project is `tests/Server.IntegrationTests`. Create further test projects alongside the first behavior they verify, rather than adding empty projects only to match the architecture. Use one test framework consistently: **xUnit**, through the `xunit.v3` package. [Microsoft's xUnit guide](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-csharp-with-xunit) shows the basic project setup.

## Test platform and commands

Tests run on Microsoft Testing Platform v2, the default of the `xunit.v3` 4.x packages. `PolskiStreamerSymulatorApp/global.json` switches `dotnet test` to its Microsoft Testing Platform mode, and `dotnet` reads that file only when a command runs from `PolskiStreamerSymulatorApp/` or below. Run the tests from that folder:

    dotnet test --solution PolskiStreamerSymulatorApp.sln

In this mode, pass a solution with `--solution` and a single project with `--project`. Running `dotnet test` from the repository root falls back to the VSTest mode, which cannot run these projects. Each test project keeps `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio`, as xUnit recommends, so IDE test explorers that still use VSTest can discover the tests. Test projects are executables and set `<OutputType>Exe</OutputType>`.
```

Directly below the table under `## Proposed test projects`, add:

```text
`Server.IntegrationTests` exists since work package F1. Add the other projects with the first behavior they verify.
```

In the `## Package shortlist` table, replace these rows:

```text
| General .NET tests | `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` | First test project |
| Code coverage | `coverlet.collector` | CI coverage reporting, after useful tests exist |
| Browser journeys | `Microsoft.Playwright.Xunit` | First complete hosted flow |
| ASP.NET Core integration | `Microsoft.AspNetCore.Mvc.Testing` | Server project exists |
| Property-based checks | `FsCheck.Xunit` or equivalent | After domain transition API stabilizes |
```

with:

```text
| General .NET tests | `xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` | Added with `Server.IntegrationTests` |
| Code coverage | `Microsoft.Testing.Extensions.CodeCoverage`; Coverlet's collector does not run under Microsoft Testing Platform | CI coverage reporting, after useful tests exist |
| Browser journeys | `Microsoft.Playwright.Xunit.v3` | First complete hosted flow |
| ASP.NET Core integration | `Microsoft.AspNetCore.Mvc.Testing` | Added with `Server.IntegrationTests` |
| Property-based checks | `FsCheck.Xunit.v3` or equivalent | After domain transition API stabilizes |
```

Keep the `Blazor components` and `SQLite integration database` rows unchanged.

- [ ] **Step 9: Update the deployment guide for tests and health**

In `docs/deployment.md`, replace

```text
The workflow runs `dotnet test` and builds the .NET container image from the exact checked-out commit,
```

with

```text
The workflow runs `dotnet test --solution` from `PolskiStreamerSymulatorApp/`, where `global.json` selects the Microsoft Testing Platform mode, and builds the .NET container image from the exact checked-out commit,
```

In the same file, append this text to the end of readiness gate 2 (the item that starts with "`/health/live` and `/health/ready`"):

```text
 Both endpoints exist: `/health/live` runs no checks, and `/health/ready` runs the checks tagged `ready`. The published-catalogue check joins that tag in S2; keep the analytics check untagged.
```

- [ ] **Step 10: Commit**

```bash
cd .. && git add PolskiStreamerSymulatorApp/Directory.Packages.props PolskiStreamerSymulatorApp/global.json PolskiStreamerSymulatorApp/PolskiStreamerSymulatorApp.sln PolskiStreamerSymulatorApp/src/Server PolskiStreamerSymulatorApp/tests/Server.IntegrationTests .github/workflows/deploy.yml docs/testing-strategy.md docs/deployment.md && git commit -m "Add Server host with health endpoints and integration tests" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Serve the Blazor client from the Server, test first

**Files:**
- Create: `PolskiStreamerSymulatorApp/tests/Server.IntegrationTests/ClientHostingTests.cs`, `PolskiStreamerSymulatorApp/src/Server/Properties/launchSettings.json`
- Modify: `PolskiStreamerSymulatorApp/Directory.Packages.props`, `PolskiStreamerSymulatorApp/src/Server/Server.csproj`, `PolskiStreamerSymulatorApp/src/Server/Program.cs`
- Modify: `PolskiStreamerSymulatorApp/src/BlazorApp/BlazorApp.csproj`, `PolskiStreamerSymulatorApp/src/BlazorApp/wwwroot/index.html`
- Modify: `docs/technical-design.md`, `docs/library-guide.md`, `docs/README.md`, `README.md`, `AGENTS.md`

**Interfaces:**
- Consumes: `MapHealthEndpoints` and the `Program` class from Task 2.
- Produces: the final `Program.cs` pipeline; `index.html` that loads `_framework/blazor.webassembly.js` and `PolskiStreamerSymulatorApp.BlazorApp.styles.css`.

- [ ] **Step 1: Write the failing client hosting tests**

Create `tests/Server.IntegrationTests/ClientHostingTests.cs`:

```csharp
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PolskiStreamerSymulatorApp.Server.IntegrationTests;

public sealed partial class ClientHostingTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task RootServesHostPageThatLoadsBlazorBootScript()
    {
        using HttpClient client = factory.CreateClient();

        string hostPage = await GetHostPageAsync(client, "/");

        Assert.Contains("<script src=\"_framework/blazor.webassembly.js\"></script>", hostPage, StringComparison.Ordinal);
        Assert.DoesNotContain("#[.{fingerprint}]", hostPage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EveryLocalAssetReferencedByHostPageIsServed()
    {
        using HttpClient client = factory.CreateClient();
        string hostPage = await GetHostPageAsync(client, "/");
        string[] assetPaths = [.. LocalAssetReference().Matches(hostPage).Select(match => match.Groups["path"].Value)];

        Assert.NotEmpty(assetPaths);
        foreach (string assetPath in assetPaths)
        {
            using HttpResponseMessage response = await client.GetAsync(assetPath, TestContext.Current.CancellationToken);

            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{assetPath} returned {(int)response.StatusCode}.");
        }
    }

    [Theory]
    [InlineData("/counter")]
    [InlineData("/career/week/3")]
    public async Task ClientRouteFallsBackToHostPage(string path)
    {
        using HttpClient client = factory.CreateClient();

        await GetHostPageAsync(client, path);
    }

    [Fact]
    public async Task MissingStaticFileReturnsNotFound()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/_framework/blazor.webassembly.outdated.js", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<string> GetHostPageAsync(HttpClient client, string path)
    {
        using HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("<div id=\"app\">", body, StringComparison.Ordinal);

        return body;
    }

    [GeneratedRegex(@"(?:href|src)=""(?<path>(?!https?:|//|#|\.)[^""]+)""")]
    private static partial Regex LocalAssetReference();
}
```

The asset pattern skips absolute URLs, fragments, and the template's `href="."` reload link; it keeps `<base href="/">`, which simply requests the host page again.

- [ ] **Step 2: Run the tests and see them fail**

Run: `dotnet test --project tests/Server.IntegrationTests/Server.IntegrationTests.csproj`

Expected: `total: 11`, `failed: 4`, `succeeded: 7`. The root, asset, and both route tests report `Actual: NotFound`. `MissingStaticFileReturnsNotFound` already passes and guards the fallback from now on; the six health tests still pass.

- [ ] **Step 3: Wire client hosting into the Server**

In `Directory.Packages.props`, add this line directly after the `Microsoft.AspNetCore.Components.WebAssembly.DevServer` line, so it sits before `Microsoft.AspNetCore.Mvc.Testing`:

```xml
    <PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="10.0.12" />
```

Replace the whole content of `src/Server/Server.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <RootNamespace>PolskiStreamerSymulatorApp.Server</RootNamespace>
    <AssemblyName>PolskiStreamerSymulatorApp.Server</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Application\Application.csproj" />
    <ProjectReference Include="..\BlazorApp\BlazorApp.csproj" />
    <ProjectReference Include="..\Contracts\Contracts.csproj" />
    <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

Replace the whole content of `src/Server/Program.cs` with:

```csharp
using PolskiStreamerSymulatorApp.Application;
using PolskiStreamerSymulatorApp.Infrastructure;
using PolskiStreamerSymulatorApp.Server.Health;

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

Create `src/Server/Properties/launchSettings.json`:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "inspectUri": "{wsProtocol}://{url.hostname}:{url.port}/_framework/debug/ws-proxy?browser={browserInspectUri}",
      "applicationUrl": "http://localhost:5180",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "inspectUri": "{wsProtocol}://{url.hostname}:{url.port}/_framework/debug/ws-proxy?browser={browserInspectUri}",
      "applicationUrl": "https://localhost:7180;http://localhost:5180",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

- [ ] **Step 4: Run the tests and see the host page problems**

Run: `dotnet test --project tests/Server.IntegrationTests/Server.IntegrationTests.csproj`

Expected: `total: 11`, `failed: 2`, `succeeded: 9`.
- `RootServesHostPageThatLoadsBlazorBootScript` fails because the build serves a processed page that loads a fingerprinted boot script. A publish would ship the raw placeholder instead, as the spec explains.
- `EveryLocalAssetReferencedByHostPageIsServed` fails with `BlazorApp.styles.css returned 404.`

- [ ] **Step 5: Switch the client to the classic host page**

In `src/BlazorApp/BlazorApp.csproj`, delete this line:

```xml
    <OverrideHtmlAssetPlaceholders>true</OverrideHtmlAssetPlaceholders>
```

In `src/BlazorApp/wwwroot/index.html`, make four edits:

1. Delete the line `    <link rel="preload" id="webassembly" />`.
2. Delete the line `    <script type="importmap"></script>`.
3. Replace `<link href="BlazorApp.styles.css" rel="stylesheet" />` with `<link href="PolskiStreamerSymulatorApp.BlazorApp.styles.css" rel="stylesheet" />`.
4. Replace `<script src="_framework/blazor.webassembly#[.{fingerprint}].js"></script>` with `<script src="_framework/blazor.webassembly.js"></script>`.

The `<head>` then reads:

```html
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>BlazorApp</title>
    <base href="/" />
    <link rel="stylesheet" href="lib/bootstrap/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="css/app.css" />
    <link rel="icon" type="image/png" href="favicon.png" />
    <link href="PolskiStreamerSymulatorApp.BlazorApp.styles.css" rel="stylesheet" />
</head>
```

- [ ] **Step 6: Run the tests and see them pass**

Run: `dotnet test --project tests/Server.IntegrationTests/Server.IntegrationTests.csproj`

Expected: `Test run summary: Passed!` with `total: 11` and `failed: 0`.

- [ ] **Step 7: Update the technical design**

In `docs/technical-design.md`, replace the first paragraph under `## Observed solution and approved direction` with:

```text
The repository contains a .NET 10 solution under `PolskiStreamerSymulatorApp/`. `global.json` requests SDK `10.0.201` with `latestFeature` roll-forward and switches `dotnet test` to its Microsoft Testing Platform mode; the installed `10.0.401` SDK builds the solution. The projects are `Domain`, `Application`, `Infrastructure`, `Contracts`, the ASP.NET Core `Server`, the standalone Blazor WebAssembly `BlazorApp`, and `tests/Server.IntegrationTests`. Central package management and warnings-as-errors are enabled.
```

In the second paragraph, replace the sentence

```text
The server and contract projects below are target architecture, not existing code.
```

with

```text
The `Server` and `Contracts` projects exist since work package F1. The Server hosts the client and the health endpoints; Wolverine handlers and the SQLite catalogue arrive in later packages.
```

Rename the heading `## Target project graph` to `## Project graph`. In the paragraph below the graph, delete the sentence

```text
`Contracts` and `Server` are proposed new projects.
```

and replace the sentence

```text
[Microsoft documents](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/?view=aspnetcore-10.0) hosted WebAssembly.
```

with

```text
`Server` references `BlazorApp` and serves its static web assets with `MapStaticAssets`, falling back to `index.html` for client routes. Microsoft's .NET 10 documentation covers standalone static hosting and Blazor Web Apps rather than this hosted shape, but its [static files guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/static-files?view=aspnetcore-10.0) confirms that `MapStaticAssets` replaces `UseBlazorFrameworkFiles` for WebAssembly framework files. The client keeps the classic `index.html`, which loads `_framework/blazor.webassembly.js`. With `OverrideHtmlAssetPlaceholders`, a publish through the Server ships unprocessed placeholders and the client cannot start, so fingerprinted boot script names stay off until the SDK supports this case.
```

- [ ] **Step 8: Update the library guide and status texts**

In `docs/library-guide.md`, add this row at the end of the first table:

```text
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | Adopted with the Server in F1 | `UseWebAssemblyDebugging` for client debugging in Development; `MapStaticAssets` serves the client files, so `UseBlazorFrameworkFiles` is not used |
```

In `docs/README.md`, replace

```text
This is a **pre-production baseline** updated 2026-10-05.
```

with

```text
This is a **pre-production baseline** updated 2026-10-06.
```

and replace

```text
`PolskiStreamerSymulatorApp/` contains a .NET 10 / Blazor WebAssembly skeleton, but no game behavior or server project yet.
```

with

```text
`PolskiStreamerSymulatorApp/` contains an ASP.NET Core server that hosts the Blazor WebAssembly client and health endpoints, but no game behavior yet.
```

In `README.md` at the repository root, replace

```text
This repository contains the **pre-production specification** and a .NET 10 / Blazor WebAssembly solution skeleton under `PolskiStreamerSymulatorApp/`; it is not a playable game.
```

with

```text
This repository contains the **pre-production specification** and a .NET 10 solution under `PolskiStreamerSymulatorApp/`, where an ASP.NET Core server hosts the Blazor WebAssembly client and health endpoints; it is not a playable game yet.
```

replace

```text
The planned CQRS architecture adds an ASP.NET Core server to the existing `Domain`, `Application`, `Infrastructure`, and `BlazorApp` projects.
```

with

```text
The CQRS architecture uses the `Domain`, `Application`, `Infrastructure`, `Contracts`, `Server`, and `BlazorApp` projects.
```

and replace

```text
Infrastructure files are present under `deploy/`, but the first deployment requires the future ASP.NET Core `Server` project and the remaining backup configuration.
```

with

```text
Infrastructure files are present under `deploy/`; the first deployment still requires the remaining readiness gates in the deployment guide, including the SQLite catalogue and backup configuration.
```

In `AGENTS.md`, replace

```text
`PolskiStreamerSymulatorApp/` currently contains only a .NET/Blazor skeleton.
```

with

```text
`PolskiStreamerSymulatorApp/` currently contains the project skeleton: an ASP.NET Core `Server` that hosts the Blazor client and health endpoints, with no game behavior yet.
```

- [ ] **Step 9: Build and test the whole solution**

Run:

```bash
dotnet build PolskiStreamerSymulatorApp.sln
dotnet test --solution PolskiStreamerSymulatorApp.sln -c Release
```

Expected: `0 Warning(s)`, `0 Error(s)`, then `Passed!` with `total: 11` and `failed: 0`.

- [ ] **Step 10: Commit**

```bash
cd .. && git add PolskiStreamerSymulatorApp/Directory.Packages.props PolskiStreamerSymulatorApp/src/Server PolskiStreamerSymulatorApp/src/BlazorApp PolskiStreamerSymulatorApp/tests/Server.IntegrationTests docs/technical-design.md docs/library-guide.md docs/README.md README.md AGENTS.md && git commit -m "Serve the Blazor client from the Server" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Container entrypoint, published-output checks, and completion records

**Files:**
- Modify: `deploy/Dockerfile`, `.dockerignore`
- Modify: `docs/deployment.md`, `docs/delivery-plan.md`, `docs/decisions.md`, `docs/README.md`
- Create, not committed: `.publish-check/boot-check.mjs` at the repository root, a folder `.gitignore` already ignores and Step 7 deletes

**Interfaces:**
- Consumes: the published Server from Tasks 2 and 3, assembly `PolskiStreamerSymulatorApp.Server.dll`.
- Produces: a Dockerfile that starts that assembly on port 8080.

- [ ] **Step 1: Show that the current entrypoint name is wrong**

Run:

```bash
dotnet publish src/Server/Server.csproj -c Release -o ../.publish-check
ls ../.publish-check/Server.dll ../.publish-check/PolskiStreamerSymulatorApp.Server.dll
```

Expected: `Server.dll` is missing and `PolskiStreamerSymulatorApp.Server.dll` exists. `.publish-check/` is already listed in `.gitignore`.

- [ ] **Step 2: Fix the Dockerfile and the build context**

Replace the whole content of `deploy/Dockerfile` with:

```dockerfile
# Multi-stage publish of the ASP.NET Core Server, which also serves the Blazor WebAssembly client.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish PolskiStreamerSymulatorApp/src/Server/Server.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
USER 1654
EXPOSE 8080
ENTRYPOINT ["dotnet", "PolskiStreamerSymulatorApp.Server.dll"]
```

In `.dockerignore`, add these two lines directly after the `**/obj` line:

```text
**/artifacts
.publish-check
```

`Directory.Build.props` sends local builds to `PolskiStreamerSymulatorApp/artifacts`, which holds hundreds of megabytes of Windows build output that must not enter the image build.

- [ ] **Step 3: Check the published output in Production**

Run the published server in the background. The subshell starts it from the publish folder, which becomes its content root, and leaves your shell in `PolskiStreamerSymulatorApp/`:

```bash
(cd ../.publish-check && ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_HTTP_PORTS=18080 dotnet PolskiStreamerSymulatorApp.Server.dll) &
```

Wait until `curl -sf http://localhost:18080/health/live` succeeds, then run:

```bash
for path in /health/live /health/ready / /career/week/3 /_framework/blazor.webassembly.js /PolskiStreamerSymulatorApp.BlazorApp.styles.css /_framework/blazor.webassembly.outdated.js; do echo "$path $(curl -s -o /dev/null -w '%{http_code}' "http://localhost:18080$path")"; done
curl -s http://localhost:18080/ | grep -o '<script src="[^"]*"'
```

Expected: 200 for every path except `/_framework/blazor.webassembly.outdated.js`, which returns 404. The host page loads `<script src="_framework/blazor.webassembly.js"`.

- [ ] **Step 4: Check that the published client boots in a browser**

Save this throwaway script as `../.publish-check/boot-check.mjs`. Git ignores that folder, so the script is never committed:

```javascript
// Throwaway smoke check: does the Blazor client boot in a real browser?
// Usage: node boot-check.mjs <url> <chromePath> <profileDir>
import { spawn } from "node:child_process";

const [url, chromePath, profileDir] = process.argv.slice(2);
const port = 9333;
const chrome = spawn(chromePath, [
  "--headless=new",
  "--disable-gpu",
  "--no-first-run",
  `--user-data-dir=${profileDir}`,
  `--remote-debugging-port=${port}`,
  "about:blank",
], { stdio: "ignore" });

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
let targets = [];
for (let i = 0; i < 50 && targets.length === 0; i++) {
  await sleep(200);
  try {
    targets = (await (await fetch(`http://127.0.0.1:${port}/json/list`)).json()).filter((t) => t.type === "page");
  } catch {
    // Chrome is still starting.
  }
}
if (targets.length === 0) {
  console.log("RESULT: chrome did not start");
  chrome.kill();
  process.exit(2);
}

const ws = new WebSocket(targets[0].webSocketDebuggerUrl);
await new Promise((r) => ws.addEventListener("open", r, { once: true }));
let nextId = 1;
const pending = new Map();
const problems = [];
ws.addEventListener("message", (event) => {
  const msg = JSON.parse(event.data);
  if (msg.id && pending.has(msg.id)) {
    pending.get(msg.id)(msg);
    pending.delete(msg.id);
    return;
  }
  if (msg.method === "Runtime.exceptionThrown") {
    problems.push(`exception: ${msg.params.exceptionDetails.exception?.description ?? msg.params.exceptionDetails.text}`);
  }
  if (msg.method === "Runtime.consoleAPICalled" && ["error", "warning"].includes(msg.params.type)) {
    problems.push(`console.${msg.params.type}: ${msg.params.args.map((a) => a.value ?? a.description).join(" ")}`);
  }
  if (msg.method === "Network.responseReceived" && msg.params.response.status >= 400) {
    problems.push(`http ${msg.params.response.status}: ${msg.params.response.url}`);
  }
});
const send = (method, params = {}) => new Promise((resolve) => {
  const id = nextId++;
  pending.set(id, resolve);
  ws.send(JSON.stringify({ id, method, params }));
});

await send("Runtime.enable");
await send("Network.enable");
await send("Page.enable");
await send("Page.navigate", { url });

let heading = null;
for (let i = 0; i < 60 && !heading; i++) {
  await sleep(500);
  const result = await send("Runtime.evaluate", {
    expression: "document.querySelector('article h1')?.textContent ?? null",
    returnByValue: true,
  });
  heading = result.result?.result?.value ?? null;
}

console.log(`RESULT: heading=${JSON.stringify(heading)}`);
for (const p of problems) {
  console.log(`PROBLEM: ${p}`);
}
ws.close();
chrome.kill();
process.exit(heading ? 0 : 1);
```

Run it with Node 24 and the installed Chrome, using a fresh browser profile inside the same folder:

```bash
node ../.publish-check/boot-check.mjs http://localhost:18080/ "C:/Program Files/Google/Chrome/Application/chrome.exe" "$(cygpath -w ../.publish-check/chrome-profile)"
```

Expected: `RESULT: heading="Hello, world!"` and no `PROBLEM:` lines.

Stop the published server afterwards:

```bash
powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='dotnet.exe'\" | Where-Object { \$_.CommandLine -like '*PolskiStreamerSymulatorApp.Server.dll*' } | ForEach-Object { Stop-Process -Id \$_.ProcessId -Force }"
```

- [ ] **Step 5: Check the container image**

Run `docker info`. If the daemon is not running, record that this step was skipped and continue with Step 6. Otherwise, from the repository root:

```bash
docker build -f deploy/Dockerfile -t pss-server:f1-check .
docker run -d --rm --name pss-f1-check -p 18081:8080 pss-server:f1-check
```

Wait until `curl -sf http://localhost:18081/health/live` succeeds, then repeat Step 3's request loop against port 18081 and run the boot check against `http://localhost:18081/`. Expected: the same results as Steps 3 and 4. Then clean up:

```bash
docker stop pss-f1-check
docker image rm pss-server:f1-check
```

The first build downloads the .NET SDK and ASP.NET base images and takes several minutes.

- [ ] **Step 6: Record completion in the documents**

In `docs/deployment.md`, replace

```text
The current solution has no `Server` project, container image, health endpoints, or event catalogue implementation yet.
```

with

```text
The solution has the `Server` project with `/health/live` and `/health/ready`, and `deploy/Dockerfile` builds its image; there is no published image, SQLite volume, or event catalogue implementation yet.
```

replace the table row

```text
| `deploy/Dockerfile` | Future .NET 10 multi-stage publish of `Server`. It cannot build until that project exists. |
```

with

```text
| `deploy/Dockerfile` | .NET 10 multi-stage publish of `Server`, started as `PolskiStreamerSymulatorApp.Server.dll` on port 8080. `.dockerignore` keeps the local `artifacts` build folder out of the build context. |
```

replace

```text
6. Complete the `Server` project, health endpoints, SQLite volume mount and catalogue migration/import path described below.
```

with

```text
6. Complete the SQLite volume mount and catalogue migration/import path described below; the `Server` project and health endpoints already exist.
```

and append this sentence to the end of readiness gate 1 (the item that starts with "`Server` and `Contracts` projects"):

```text
 Done in F1: the Server hosts the client, and `tests/Server.IntegrationTests` is in the solution.
```

In `docs/delivery-plan.md`, replace

```text
A .NET 10 / Blazor solution skeleton exists, but no game behavior has been implemented.
```

with

```text
Work package F1 is complete: the ASP.NET Core Server hosts the Blazor client and health endpoints. No game behavior has been implemented yet.
```

and add this paragraph directly after the ordered work package table, before the paragraph that starts with "The **first playable checkpoint**":

```text
**Progress:** F1 was completed on 2026-10-06; its design spec and implementation plan are in `docs/superpowers/`. The health-endpoint integration test planned for F3 was written in F1 under test-driven development, so F3 still owes the CI build and test gate.
```

In `docs/decisions.md`, replace the D-14 row

```text
| D-14 | Server | Confirmed | Add an ASP.NET Core server. The proposed graph also adds a small shared `Contracts` project. |
```

with

```text
| D-14 | Server | Confirmed | Add an ASP.NET Core server and a small shared `Contracts` project for HTTP DTOs. Both exist since work package F1. |
```

In `docs/README.md`, replace the testing strategy row

```text
| [Testing strategy](testing-strategy.md) | Proposed test projects, scenarios, tools, and gates | Proposed baseline |
```

with

```text
| [Testing strategy](testing-strategy.md) | Test projects, test platform, scenarios, tools, and gates | Baseline; Server integration tests run on Microsoft Testing Platform |
```

and append this paragraph to the end of the `## Change policy` section:

```text
Work-package design specs and implementation plans live in `docs/superpowers/specs/` and `docs/superpowers/plans/`. They record how a package was built; the documents above stay authoritative for rules and contracts.
```

- [ ] **Step 7: Final verification**

From `PolskiStreamerSymulatorApp/`, run:

```bash
dotnet build PolskiStreamerSymulatorApp.sln
dotnet test --solution PolskiStreamerSymulatorApp.sln -c Release
dotnet list src/BlazorApp/BlazorApp.csproj reference
```

Expected: `0 Warning(s)` and `0 Error(s)`; `Passed!` with `total: 11` and `failed: 0`; BlazorApp lists only `..\Contracts\Contracts.csproj`.

Remove the publish folder: `rm -rf ../.publish-check`

- [ ] **Step 8: Commit**

```bash
cd .. && git add deploy/Dockerfile .dockerignore docs/deployment.md docs/delivery-plan.md docs/decisions.md docs/README.md && git commit -m "Fix container entrypoint and record F1 completion" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>" && git status --short
```

Expected: the commit succeeds and `git status --short` prints nothing.
