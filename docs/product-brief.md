# Product brief

## Product statement

**Polski Streamer Symulator** is a single-player, text-forward browser career simulator. The player starts as an unknown Polish streamer, accepts a generated name or enters their own, and makes weekly choices with percentage-based consequences. The three game statistics are **money, viewers, and drama**. One week may contain several events; some outcomes lead to future stories or special early endings. The first published configuration lasts up to 52 weeks and ends early in defeat if money reaches -1,000 PLN; these numeric values are versioned in SQLite for future configurations. A single final score combines channel audience and net profit after expenses equally; drama shapes the career but does not score directly. The appeal is the story of a career shaped by choices and dice rolls.

The title and gameplay are original. The reference game establishes the broad career-simulation pattern; it does not supply this game's content, formulas, assets, or code.

## Audience and experience goals

- Polish- and English-speaking players curious about Polish internet culture, including people who do not stream themselves. Both language versions should make a choice understandable even when an allusion to a real online story is unfamiliar.
- A complete first run should be possible without creating an account or learning real streaming platforms.
- Decisions should create understandable trade-offs. A choice can bring viewers or money while raising drama, and a calmer response can give up short-term reach. Every choice has visible outcome chances. Money reports separate sponsors, donations, weekly subscriptions, and expenses.
- A bad week should be recoverable. Luck may change the story, but it must not erase the effect of repeated good choices.
- The game should be readable and usable on a phone, where many players will encounter it.
- Humor should satirize recognizable incentives and situations in Polish internet culture.

## Product and business assumptions

Confirmed first-version choices and proposed operational details are tracked separately in `decisions.md`:

- Free, single-player browser game in Polish and English, with no account, multiplayer, real money purchases, or ads in the MVP.
- No real platform API integration; metrics and sponsors are fictional simulation data.
- Career saves stay in the player's browser. The server uses SQLite for shared events, typed game parameters, and streamer-name parts; it does not persist chosen names, players, or runs. Saves do not sync through an account; first-version file export/import lets a player move a save manually between devices or browser profiles.
- First-party aggregate analytics for app opens, progress by week, endings, and choices will be collected by default with a clear player notice and no separate in-game opt-in step. The proposed separate `analytics.db` SQLite file will contain counters rather than player saves or names; no third-party browser tracker is planned. Keep counters for 12 months and node logs for 14 days. Review the notice and applicable consent requirements before production collection. See [logging and analytics](observability-and-analytics.md).
- Fictional names, brands, and platforms with sharp, recognizable allusions to particular Polish internet stories about public creators. Content review must check each allusion before publication, especially if it could identify a private person or imply an unverified allegation.

## Scope

### MVP

One character, one channel, an initial 52-week configuration, one probability-based action per week followed by zero to three events, three core statistics, a two-part suggested streamer name with custom entry, weekly reports, one autosave and three manual browser save slots, local file export/import, a bankruptcy, special early ending, or configured-end recap with one score, and replay. Paid choices can enter debt and may temporarily cross the bankruptcy limit if their rolled result later rescues the run. The first special ending reasons are permanent ban and channel closure (defeats), and retirement from streaming (neutral). The player sets only the streamer name; there is no channel-name, content-theme, platform, or archetype choice at setup. There are no separate viewer milestones; some events are one-time, others repeat after cooldown, and some results may unlock a random chance of a later follow-up that competes equally with ordinary events. All player-facing screens and published event text must be available in Polish and English. Four working weekly actions are accepted for testing; their costs and odds remain provisional. An authenticated content admin panel is part of the first version. The creator is considering roughly 200–400 original events for the first public release, with the exact commitment deferred; 20 Polish draft examples are documented now and need English adaptation before publication.

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

Simulating an actual platform's algorithms, predicting real creator income, verbatim reenactments of real controversies, unsupported factual claims about real people, real-time chat, player accounts, social networking, and a live-service economy.
