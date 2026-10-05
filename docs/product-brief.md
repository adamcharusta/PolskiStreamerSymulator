# Product brief

## Product statement

**Polski Streamer Symulator** is a single-player, text-forward browser career simulator. The player starts as an unknown Polish streamer and chooses what to stream, how intensely to work, and where to invest scarce time and money. Each simulated week produces a readable report: audience, earnings, energy, reputation, and an event that can create an opportunity or a setback. The appeal is the story of a career shaped by choices and chance.

The title and gameplay are original. The reference game establishes the broad career-simulation pattern; it does not supply this game's content, formulas, assets, or code.

## Audience and experience goals

- Polish-speaking players familiar with internet culture, including people who do not stream themselves.
- A complete first run should be possible without creating an account or learning real streaming platforms.
- Decisions should create understandable trade-offs. More hours can raise reach and income while draining energy; a sponsored campaign can improve cash while hurting trust if it mismatches the audience.
- A bad week should be recoverable. Luck may change the story, but it must not erase the effect of repeated good choices.
- The game should be readable and usable on a phone, where many players will encounter it.
- Humor should satirize recognizable incentives and situations in Polish internet culture.

## Product and business assumptions

These are proposed defaults pending creator confirmation in `decisions.md`:

- Free, single-player, Polish-language browser game, with no account, multiplayer, real money purchases, or ads in the MVP.
- No real platform API integration; metrics and sponsors are fictional simulation data.
- Career saves stay in the player's browser. The server uses SQLite only for the shared event catalogue and does not persist player or run data. Saves do not sync between devices or browser profiles; export/import can be added later.
- No analytics or third-party tracking at launch. If analytics is later proposed, document purpose, consent, retention, and privacy implications first.
- Fictional brands and people. Cultural satire targets situations and incentives, not named private individuals.

## Scope

### MVP

One character, one channel, a 52-week first career year, four content formats, three workload choices, three starting archetypes, weekly reports, a small event library, one optional strategic action per week, save/load, an end-of-year summary, and replay. This is enough to test whether the central loop is fun.

### Later candidates

Multi-year careers, competing creators, collaboration chains, platform switching, staff, larger sponsor systems, equipment specialization, achievements, shareable career cards, offline installation, and richer narrative event packs. These are not MVP commitments.

## Success criteria for a playable MVP

- A new player understands the next choice and its likely trade-off without external instructions.
- A normal run has multiple viable paths: growth, sustainable income, and community trust.
- The same seed and same choices produce the same outcome, making bugs reproducible.
- A complete run can be finished without a dead end, soft lock, or lost save.
- All essential actions work with a keyboard and at a 320 CSS pixel wide viewport.
- In playtests, players can explain at least one meaningful consequence of their choices and want to try a different strategy in another run.

## Non-goals for the first version

Simulating an actual platform's algorithms, predicting real creator income, recreating real controversies, real-time chat, online accounts, social networking, and a live-service economy.
