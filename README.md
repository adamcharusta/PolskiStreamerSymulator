# Polski Streamer Symulator

An original, Polish-language browser game about building a streaming career. The player accepts a suggested streamer name or enters their own, then makes weekly decisions with percentage-based outcomes while managing money, viewers, and drama. The first published configuration lasts up to 52 weeks; a balance of -1,000 PLN ends the run in defeat. Money reports separate sponsors, donations, weekly subscriptions, and expenses. The final score combines channel audience and net profit after expenses equally.

This repository contains the **pre-production specification** and a .NET 10 / Blazor WebAssembly solution skeleton under `PolskiStreamerSymulatorApp/`; it is not a playable game. All project documentation and agent guidance are in English; the planned player-facing game is in Polish.

Start with [the documentation index](docs/README.md). The [game rules](docs/game-design.md), [technical design](docs/technical-design.md), and [decision log](docs/decisions.md) are the main implementation references.

The planned CQRS architecture adds an ASP.NET Core server to the existing `Domain`, `Application`, `Infrastructure`, and `BlazorApp` projects. The proposed SQL Server setup holds shared weekly actions, events, versioned game parameters, and streamer-name parts; chosen names and careers stay in browser saves. See the technical design before changing project references.

The [logging and analytics design](docs/observability-and-analytics.md) proposes JSON server logs, runtime metrics, and a separate SQL Server database holding only aggregate gameplay counters. It is a proposal, not an implemented service.

The [deployment guide](docs/deployment.md) describes the chosen single-VPS k3s setup, Ansible provisioning, and manually triggered GitHub Actions workflow. Infrastructure files are present under `deploy/`, but the first deployment requires the future ASP.NET Core `Server` project and an off-VPS backup destination.
