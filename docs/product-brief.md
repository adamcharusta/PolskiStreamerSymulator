# Product brief

## Product statement

**Polski Streamer Symulator** is a single-player, text-forward browser career simulator. The player starts as an unknown Polish streamer, accepts a generated name or enters their own, and makes weekly choices with percentage-based consequences. The three game statistics are **money, viewers, and drama**. Events depend on these statistics and may lead to opportunities or setbacks. The first published configuration lasts up to 52 weeks and ends early in defeat if money reaches -1,000 PLN; these numeric values are versioned in SQL Server for future configurations. A single final score combines channel audience and net profit after expenses equally; drama shapes the career but does not score directly. The appeal is the story of a career shaped by choices and dice rolls.

The title and gameplay are original. The reference game establishes the broad career-simulation pattern; it does not supply this game's content, formulas, assets, or code.

## Audience and experience goals

- Polish-speaking players familiar with internet culture, including people who do not stream themselves.
- A complete first run should be possible without creating an account or learning real streaming platforms.
- Decisions should create understandable trade-offs. A choice can bring viewers or money while raising drama, and a calmer response can give up short-term reach. Every choice has visible outcome chances. Money reports separate sponsors, donations, weekly subscriptions, and expenses.
- A bad week should be recoverable. Luck may change the story, but it must not erase the effect of repeated good choices.
- The game should be readable and usable on a phone, where many players will encounter it.
- Humor should satirize recognizable incentives and situations in Polish internet culture.

## Product and business assumptions

These are proposed defaults pending creator confirmation in `decisions.md`:

- Free, single-player, Polish-language browser game, with no account, multiplayer, real money purchases, or ads in the MVP.
- No real platform API integration; metrics and sponsors are fictional simulation data.
- Career saves stay in the player's browser. The server uses SQL Server for shared events, typed game parameters, and streamer-name parts; it does not persist chosen names, players, or runs. Saves do not sync between devices or browser profiles; export/import can be added later.
- First-party aggregate analytics are proposed for app opens, progress by week, endings, and choices. The proposed separate `PssAnalytics` database contains counters rather than player saves or names; no third-party browser tracker is planned. The privacy notice, retention, and applicable consent requirements must be reviewed before production collection. See [logging and analytics](observability-and-analytics.md).
- Fictional brands and people. Cultural satire targets situations and incentives, not named private individuals.

## Scope

### MVP

One character, one channel, an initial 52-week configuration, one probability-based action per week followed by a possible event response, three core statistics, a two-part suggested streamer name with custom entry, weekly reports, save/load, a bankruptcy defeat or configured-end recap with one score, and replay. There are no starting archetypes. The exact weekly action list is still proposed. The event-content target is at least 200 original events later; 20 draft examples are documented now and will be reviewed before publication.

### Later candidates

Multi-year careers, competing creators, collaboration chains, platform switching, staff, larger sponsor systems, achievements, shareable career cards, offline installation, and richer narrative event packs. These are not MVP commitments.

## Success criteria for a playable MVP

- A new player understands the next choice and its likely trade-off without external instructions.
- A normal run has multiple viable calm, mixed, and high-drama paths, with meaningful money and viewer trade-offs.
- The same seed and same choices produce the same outcome, making bugs reproducible.
- A complete run can be finished without a dead end, soft lock, or lost save.
- All essential actions work with a keyboard and at a 320 CSS pixel wide viewport.
- In playtests, players can explain at least one meaningful consequence of their choices and want to try a different strategy in another run.

## Non-goals for the first version

Simulating an actual platform's algorithms, predicting real creator income, recreating real controversies, real-time chat, online accounts, social networking, and a live-service economy.
