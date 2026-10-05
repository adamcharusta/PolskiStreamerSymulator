# Game design: first playable version

All rules here are the proposed MVP contract except the weekly turn and satirical tone, which the creator confirmed. Numeric constants and formulas live in [balance.md](balance.md).

## Player fantasy and objective

Build a Polish streaming career from a small channel. There is no single mandatory victory target. A player may pursue reach, a stable livelihood, or a loyal community. The first playable run covers **52 weekly turns** and ends with a career-year recap and a score by dimension. The player can then start a new run; continuation into later years is a future feature.

## Setup

The player enters a display name and channel name, chooses a content focus and one of three archetypes, then starts a run. The focus provides flavor and event eligibility without a permanent optimal choice. Archetypes create modest, visible starting differences:

| Archetype | Strength | Cost |
| --- | --- | --- |
| Entertainer | Stronger discovery from energetic content | Workload drains more energy |
| Expert | Higher content quality and sponsor fit | Slower early audience discovery |
| Community builder | Greater loyalty and recovery | Smaller viral spikes |

The UI explains these trade-offs before confirmation. Names are game data and must follow the chosen save-location and privacy rules.

## Persistent state

| Metric | Meaning | Range / unit |
| --- | --- | --- |
| Week | Current turn in the career year | 1–52 |
| Followers | Cumulative channel audience | Non-negative integer |
| Average viewers | Estimated live viewers this week | Non-negative integer; derived from audience and week performance |
| Cash | Available money after payouts and costs | Whole PLN; can be negative until an ending check |
| Energy | Ability to produce consistently | 0–100 |
| Community trust | How willing viewers are to return | 0–100 |
| Reputation | Public and sponsor perception | 0–100 |
| Equipment | Production capability | Level 1–5 |
| Career log | Choices, event, results, and metric deltas | One entry per completed week |

Followers do not directly equal viewers. Trust affects repeat viewing; reputation affects partnership opportunities; energy affects output quality. Cash is a resource, not a score by itself. No metric should become a hidden requirement for continuing a run.

## Weekly loop

1. **Plan:** Show current state, last report, and a plain-language forecast for the selected choices.
2. **Choose format:** Gaming, just chatting, challenge/IRL, or tutorial/commentary. Each favors different archetypes and event types.
3. **Choose workload:** Break (0 live hours), regular (8 hours), or intensive (20 hours). A break is a meaningful recovery choice and can still incur costs and audience drift.
4. **Optional action:** No action, community outreach, promotion, or equipment upgrade. Only actions whose cost is affordable are enabled; their costs and effects are visible. A break may still include community outreach but not promotion of a nonexistent stream.
5. **Resolve:** Apply the documented calculations in a fixed order and draw at most one random event. If an event needs a decision, stop at its choice screen before committing the week. Then calculate the final state, create a report, and advance the week.
6. **Review:** Show the result, causes of major changes, event outcome, and updated career history. The next turn starts only after review.

The player can return to planning before confirming the week. Once the week is committed, there is no undo within the run; loading an earlier manual save is allowed. Prevent double submission while resolution is in progress.

## Format identity

| Format | Expected upside | Expected downside |
| --- | --- | --- |
| Gaming | Reliable baseline and gradual loyalty | Modest discovery without a standout event |
| Just chatting | Strong community connection | Reputation risk when energy is low |
| Challenge/IRL | High discovery potential | Higher effort and event volatility |
| Tutorial/commentary | Strong credibility and long-tail growth | Slower immediate reach |

Formats are fictional abstractions. A player can change format weekly. The initial rules do not reward repetition by itself; format choice matters through its modifiers, side effects, and eligible events. Add a specialization or novelty mechanic only after playtests show it would create a useful choice.

## Weekly resolution and events

The rules engine receives current state, choices, and a seeded random source. It validates choices, calculates baseline content performance, resolves any event choice, then updates audience, cash, energy, trust, and reputation. The report stores both numerical deltas and reason codes so the UI can explain results. The event selection rules and numeric order are in `balance.md`.

Event themes include a clip spreading beyond the channel, a community in-joke, equipment trouble, a useful collaboration, a mismatched sponsor offer, a moderation problem, a creator feud parody, a charity opportunity, an algorithm change, and a creative slump. An event must offer an understandable option when it changes more than one core metric. The player must never be forced into a harmful event choice with no alternative.

## Opportunities and progression

Follower milestones at 100, 1,000, and 10,000 unlock flavor, stronger opportunities, and recap badges. They do **not** lock basic play. Sponsor offers require sufficient audience and reputation; an offer states its immediate cash reward and possible trust cost. Equipment upgrades consume cash and persist for the rest of the run. A negative week can be recovered through lighter workload, community action, or a different format.

## Endings and score

The run ends after week 52. It may also end early if cash stays below the debt floor for four consecutive weeks, representing a channel that can no longer cover costs. A break or low energy does not itself cause a game over. The final recap shows the path across all weeks, key events, and three separate ratings: reach, sustainability, and community. It highlights the player's strongest dimension; it does not collapse all strategies into one supposedly best number. The early ending should offer a clear explanation and replay.

## Information rules

- Show costs and direct effects before the player confirms a choice.
- Show approximate reach and energy forecasts as ranges, because events and discovery vary.
- Explain changes after resolution with readable reason codes, never only a raw number.
- Keep random outcomes bounded. A single roll should not wipe out an otherwise healthy career.
- Allow multiple viable routes through playtests rather than tuning solely for maximum followers.
