# Polski Streamer Symulator

An original Polish-and-English browser game about building a streaming career. The player accepts a suggested streamer name or enters their own, then makes weekly decisions with percentage-based outcomes while managing money, viewers, and drama. A week may include multiple events. The first published configuration lasts up to 52 weeks; a balance of -1,000 PLN or a special outcome can end the run early. Money reports separate sponsors, donations, weekly subscriptions, and expenses. The final score combines channel audience and net profit after expenses equally. Saves remain in the browser, with local file export/import planned for the first version.

This repository contains the **pre-production specification** and a .NET 10 / Blazor WebAssembly solution skeleton under `PolskiStreamerSymulatorApp/`; it is not a playable game. All project documentation and agent guidance are in English; the planned player-facing game supports Polish and English.

Start with [the documentation index](docs/README.md). The [game rules](docs/game-design.md), [technical design](docs/technical-design.md), and [decision log](docs/decisions.md) are the main implementation references.

The planned CQRS architecture adds an ASP.NET Core server to the existing `Domain`, `Application`, `Infrastructure`, and `BlazorApp` projects. The confirmed SQLite + EF Core choice will hold shared weekly actions, events, versioned game parameters, and streamer-name parts; chosen names and careers stay in browser saves. See the technical design before changing project references.

The [logging and analytics design](docs/observability-and-analytics.md) proposes JSON server logs, runtime metrics, and a separate SQLite database holding only aggregate gameplay counters. It is a proposal, not an implemented service.

The [deployment guide](docs/deployment.md) describes the chosen single-VPS k3s setup, Ansible provisioning, manually triggered GitHub Actions workflow, and encrypted daily backups to a second machine. Infrastructure files are present under `deploy/`, but the first deployment requires the future ASP.NET Core `Server` project and the remaining backup configuration.
