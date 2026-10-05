# Decisions and open questions

This is the canonical record of choices that affect scope or implementation. **Confirmed** means the creator stated it. **Proposed** means a working default in these documents. **Open** means implementation is blocked or materially affected until resolved.

| ID | Topic | Status | Current position / next decision |
| --- | --- | --- | --- |
| D-01 | Game title | Confirmed | Polski Streamer Symulator. |
| D-02 | Documentation language | Confirmed | Documentation, `CLAUDE.md`, and `AGENTS.md` are in English. |
| D-03 | Turn length | Confirmed | One player turn represents one streaming week. |
| D-04 | Tone | Confirmed | Satire of Polish internet culture. |
| D-05 | Technical stack | Confirmed | Existing solution targets .NET 10 with standalone Blazor WebAssembly, C#, central package management, and the Domain/Application/Infrastructure/BlazorApp split. |
| D-06 | Player-facing language | Proposed | Polish. Confirm if bilingual UI is desired. |
| D-07 | MVP duration | Proposed | One 52-week career year, with future multi-year continuation. Confirm whether the first release should already span an entire career. |
| D-08 | Business model | Proposed | Free, no ads, no accounts, no in-game purchases for MVP. Confirm any funding or monetization requirement. |
| D-09 | Save model | Confirmed | Career saves stay in the player's browser; no account or cross-device sync. Three manual slots plus autosave remain a proposed UI detail. |
| D-10 | Game depth | Proposed | Four formats, three workloads, three archetypes, optional strategic action, 12 events. Validate after paper and player testing. |
| D-11 | Real-world references | Proposed | Fictional platforms, people, and brands; satirical situations rather than direct portrayals. |
| D-12 | Online services | Proposed | An ASP.NET Core server is confirmed; no real platform APIs, analytics, or tracking are proposed for the first version. |
| D-13 | Architecture | Confirmed | CQRS with Domain for game domains, Application for use cases, Infrastructure for adapters, and BlazorApp for views. |
| D-14 | Server | Confirmed | Add an ASP.NET Core server. The proposed graph also adds a small shared `Contracts` project. |
| D-15 | Mediator | Proposed | WolverineFX on the server in mediator-only mode for the MVP. |
| D-16 | Mapping | Proposed | Mapster at the API boundary when mappings become nontrivial. |
| D-17 | Validation | Proposed | FluentValidation for Application commands; built-in Blazor form feedback and Domain invariants remain. |
| D-18 | SQLite purpose | Confirmed | SQLite stores the shared event catalogue: occurrence conditions, choices, dice probabilities, and outcomes/rewards. No player or career data is stored there. |
| D-19 | Database access | Proposed | EF Core with SQLite in Infrastructure; a versioned published catalogue prevents content edits from changing an active run. |
| D-20 | Deployment target | Confirmed | Single Ubuntu 24.04 VPS (4 vCPU, 8 GB RAM, 75 GB disk) at `polskistreamersymulator.pl`, with single-node k3s, Ansible provisioning, and manually triggered GitHub Actions deployment. Detailed topology and safeguards are in `deployment.md`. |
| D-21 | Production persistence | Proposed | One `local-path` PVC and one application replica for the event catalogue. Configure encrypted off-VPS backups before public release. |

## Questions to resolve before or during implementation

1. Should event authors update a draft through an import tool first, or is an authenticated admin UI needed in the first release? The proposed MVP uses an import/publish tool.
2. Should the MVP end after 52 weeks or support multiple years from the start? This affects pacing, event volume, and balance.
3. Is there a preferred business model or publication venue? This affects product constraints and operations.
4. How sharp should the satire be, and are there topics or creator archetypes that should be excluded? This affects the event catalogue.
5. Should the player start with a fictional platform choice, or should platform differences wait for a later version? The current proposal defers them.
6. Which encrypted off-VPS backup destination and retention period will be used before public release?

## Decision procedure

When the creator answers a question, update its row and the authoritative document in the same change. If a playtest overturns a proposed default, record the new decision and why. Keep old rationale in version control rather than leaving contradictory rules in multiple documents.
