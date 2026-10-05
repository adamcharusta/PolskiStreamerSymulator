# Logging, operational metrics, and gameplay analytics

## Status and goals

The creator wants error and information logs plus counts of app opens, where runs end, and the choices players make most often. This document proposes a **first-party, aggregate-only** implementation for the single ASP.NET Core server and VPS. It does not add accounts, server-side career saves, or third-party browser scripts. The proposed SQL Server instance hosts separate `PssCatalog` and `PssAnalytics` databases; analytics writes must never mutate published catalogue content.

Keep three data streams distinct:

| Stream | Purpose | First implementation |
| --- | --- | --- |
| Structured logs | Diagnose errors and explain server operations | Built-in `ILogger<T>` with JSON console output, collected by k3s container logs |
| Operational metrics | Watch request rate, duration, failures, and telemetry drops | Built-in ASP.NET Core/.NET meters; inspect with `dotnet-counters` initially, add an OpenTelemetry exporter when a dashboard is needed |
| Product analytics | Count opens, progress, endings, and choices over time | First-party aggregate counters in a separate `PssAnalytics` SQL Server database |

The built-in [JSON console formatter](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/console-log-formatter) and [ASP.NET Core metrics](https://learn.microsoft.com/en-us/aspnet/core/metrics/overview?view=aspnetcore-10.0) cover the first two streams without adding Serilog, Seq, Prometheus, or Grafana to the initial VPS deployment. An OpenTelemetry exporter can be added later without changing the product counter schema.

## Structured application logs

Configure the server's JSON console formatter with UTC timestamps, category, severity, stable event ID, route template, HTTP status, duration, application build, catalogue version, code rules version, and request trace ID where available. Use parameterized `ILogger<T>` calls. Set the normal application minimum to `Information` and framework categories to `Warning` unless a specific incident needs temporary detail. Central exception handling logs unhandled server failures once and returns a stable Problem Details code; expected invalid input is a warning or a validation result, not an exception dump.

Useful information events include server startup, catalogue publish/version changes, analytics writer health, and graceful shutdown. Useful warning/error events include unavailable catalogue versions, malformed saves, SQL Server write failures, failed migrations, unexpected handler exceptions, and a full analytics queue. Do not log a line for every successful gameplay choice: aggregate counters below answer that question with less noise.

Never log request or response bodies, browser save JSON, player-entered names, run IDs, IP addresses, user agents, URL query values, donation text, or raw client exception messages. A request trace ID helps connect related server log lines without becoming a player identifier. Use route **templates**, not arbitrary URL paths, in log fields and metric labels. Review reverse-proxy access logging separately so it does not undermine this policy. Container log retention and rotation must be configured on the VPS; logs are operational data, not a permanent gameplay audit.

For Blazor WebAssembly, use an error boundary and explicit handling of API/save failures. A small rate-limited first-party endpoint may accept an allowlisted client error code, route name, and build version so the server can log and count browser-side failures. It must reject free-form names, save state, raw exception text, and stack traces. Browser console details remain available during development. This reports common client errors but cannot guarantee capture if the tab crashes before sending a request.

## Product events and definitions

Emit gameplay analytics only after a command has succeeded. The server derives action and event IDs, week, catalogue version, and ending reason from the validated command/result; the browser may only submit `AppOpened` and the restricted client-error report. Do not accept arbitrary counters or choice IDs from a public telemetry endpoint.

| Event | Counted when | Dimensions retained in aggregates |
| --- | --- | --- |
| `AppOpened` | Blazor successfully starts and sends one small request per page load | UTC day |
| `RunStarted` | `StartRun` succeeds | UTC day, catalogue version |
| `WeekEntered` | A run reaches a new week | UTC day, catalogue version, week |
| `WeeklyActionChosen` | `PlanWeek` accepts an action | UTC day, catalogue version, week, stable action ID |
| `EventEncountered` | The engine selects an event, including one whose encounter cost causes defeat | UTC day, catalogue version, event ID |
| `EventPresented` | The selected event survives its encounter cost and offers response choices | UTC day, catalogue version, event ID |
| `EventOptionChosen` | `ChooseEventOption` accepts a response | UTC day, catalogue version, event ID, stable option ID |
| `WeekCompleted` | A week reaches a settled recap or terminal defeat after its action, event cost, or response; do not count a still-pending event | UTC day, catalogue version, week |
| `RunEnded` | A run reaches its configured final week or bankruptcy | UTC date and hour, catalogue version, ending week, `completed` or `bankrupt` |
| `ClientError` | A restricted browser error report is accepted | UTC day, allowlisted error code, route name, build version |

`AppOpened` measures **page loads that reached the client**, not unique people. Refreshes and new tabs count again; bots and blocked requests may distort it. `RunEnded` gives the actual ending week and UTC hour for runs that reached a game ending; reports can group hours in the operator's time zone. There is no reliable “closed the tab” event: compare `WeekEntered` and `WeekCompleted` counts by week to find likely drop-off, and label that report as an **approximation** because players may return later. Show weekly-action counts and event-option counts separately. For event responses, compare a choice count with the corresponding `EventPresented` count so an uncommon event is not mistaken for an unpopular response.

## Aggregate storage and delivery

Use a separate EF Core DbContext, connection string, and migrations for `PssAnalytics`. This database contains **aggregate counts only**. Suggested tables are `DailyCounter(DayUtc, Metric, Count)` for app opens, `RunStartCount(DayUtc, CatalogVersion, Count)`, `WeekFunnelCount(DayUtc, CatalogVersion, Week, Stage, Count)`, `WeeklyActionCount(DayUtc, CatalogVersion, Week, ActionId, Count)`, `EventExposureCount(DayUtc, CatalogVersion, EventId, Stage, Count)`, `EventOptionCount(DayUtc, CatalogVersion, EventId, OptionId, Count)`, `EndingCount(DayUtc, HourUtc, CatalogVersion, Week, Reason, Count)`, and `ClientErrorCount(DayUtc, ErrorCode, RouteName, BuildVersion, Count)`. Use unique keys on each dimension tuple and transactional update-or-insert logic. Keep action, event, and option IDs as stable catalogue IDs; do not store their changing Polish copy in analytics.

`Application` exposes a small `IAnalyticsSink` port for successful command facts. `Infrastructure` provides a bounded in-memory channel and one background SQL Server writer. The server's app-open endpoint and successful CQRS handlers enqueue small allowlisted fact batches, one batch per command; Domain simulation stays pure. Batch counter updates in transactions and use unique indexes to prevent duplicate aggregate rows. With the planned one application replica, one writer keeps write contention low. Configure bounded retries for transient SQL errors, but do not retry indefinitely. If the queue is full or the analytics database fails, log a warning, increment `analytics_dropped_total`, and let gameplay complete normally. Flush briefly on graceful shutdown. Counts are therefore operational estimates, not financial records.

To reduce double counting on immediate gameplay-command retries, keep a **bounded in-memory** deduplication cache for about 24 hours. Its key is a hash of `runId`, week, command phase, and choice ID; the raw run ID and hash are never written to `PssAnalytics` or logs. Mark a key as seen only when its facts have been accepted into the analytics queue. This is best effort: a restart or old replay can count again, and a queued fact can still be lost if the writer fails. Do not add a server-side run table merely to make analytics exact. App opens are not deduplicated across reloads.

No public analytics dashboard or query endpoint is needed initially. Provide an operator-only read-only report command on the VPS with totals by day, ending-week distribution, weekly-action ranking, event-response ranking, and a week progression funnel. Keep the reports away from the public player API. If operations later need live dashboards or alerts, export the built-in .NET meters to Prometheus/Grafana through OpenTelemetry and restrict scraping to the cluster; do not expose a metrics endpoint on the public ingress.

## Data handling and release checks

- Keep chosen streamer/channel names, browser saves, IP addresses, user agents, raw run IDs, free text, and individual gameplay histories out of `PssAnalytics`. The catalogue database remains shared game data, while the analytics database contains only counters.
- Rate-limit the app-open and client-error endpoints, limit payload size, and allow only known event/error codes. Use no third-party tracking script and no analytics cookie or persistent browser identifier in the first version.
- Set a documented retention policy before release: propose **14 days for node logs** and **12 months for daily aggregate counters**, then delete or roll them up. Include SQL Server backups of `PssAnalytics` in off-VPS backup and restore drills. Report disk usage and writer failures in operations checks.
- Publish a clear Polish privacy notice describing these counts and their purpose. Review the applicable privacy/consent requirements before enabling telemetry in production; this design does not assume that a first-party implementation is automatically exempt.
- Tests should prove that names and save payloads never enter logs or analytics storage, a retry is counted once while the dedup cache is live, a queue/database failure does not fail gameplay, and alternate catalogue versions remain separate in reports.
