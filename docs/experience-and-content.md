# Experience, interface, and content

## Screen map

| Screen | Essential information and action |
| --- | --- |
| Start | Short premise, new game, load game, and a clear note that career saves stay in this browser and can be lost if browser data is cleared |
| Setup | Editable streamer name with a two-part SQL Server suggestion and regenerate button, optional channel name/content focus, the pinned starting statistics and run length, and start confirmation; no archetype selection |
| Dashboard / plan | Current week out of the pinned total, money, viewers, drama, latest trend, one weekly action selection, percentage previews, and resolve button |
| Event choice | Situation, any encounter cost already charged, response-specific costs, possible outcomes with percentages, and confirmation |
| Weekly report | Chosen responses, dice results, viewers/drama deltas, opening and closing money, sponsor/donation/subscription/expense subtotals, event result, and continue button |
| Career history | Chronological weekly reports with milestone markers and filters by year or event type if needed |
| Save/load | Named browser-local slots, timestamps, version, overwrite confirmation, and recovery guidance |
| Final recap | Completion or bankruptcy defeat stated first, then one career score with audience and net-profit point breakdown, final money/viewers/drama, cumulative cash-flow categories, career timeline, key decisions, replay, and return to start |

The dashboard is the main screen. On narrow viewports, show the week and all three statistics before choices and secondary details. Keep a persistent route to history and saves. Avoid a full-screen wall of numbers.

## Feedback rules

- Every decision has a short explanation of its intended trade-off.
- A forecast distinguishes guaranteed costs from weighted outcomes. Show each outcome's percentage and meaningful effect before confirmation, especially for a high-drama risk.
- When an event has an automatic encounter cost, show the amount immediately as an expense already charged. Show a paid response's additional cost separately before the player selects it; disable that response if it is unaffordable and keep a free response available.
- Show the pinned bankruptcy limit, initially -1,000 PLN, near the money balance. In a choice preview, warn when its possible loss could reach the limit. If an automatic encounter cost reaches the limit, show that cost and a defeat recap immediately instead of presenting response choices. Completion after the configured final week and bankruptcy use distinct ending labels; a high score on the defeat screen does not imply victory.
- Each weekly report states: what the player chose, what happened, why the key metrics moved, and what changed for the next week.
- Show the four money categories even when a category is zero. Explain a negative net week through its expense entries; the sum of the categories must match the displayed money change.
- In the final recap, show the score calculation in plain language: final viewers contribute half of the scoring formula, and net profit from sponsors, donations, and subscriptions after expenses contributes the other half. Show the run's pinned starting balance (initially 1,500 PLN) separately from earned profit. Drama appears as career context, not a score component. The exact formula and provisional reference values live in [balance](balance.md).
- The suggested streamer name is visibly editable; a custom entry takes precedence. Regenerate only on request and keep the current text if the generator fails. Show an inline validation error for an invalid custom name. The detailed flow and vocabulary rules are in [streamer name generation](streamer-name-generation.md).
- Use both text and visual direction for increases and decreases; color alone is insufficient.
- Flag milestone unlocks at the week they occur, and show them again in the recap.
- Make saving status visible. Never imply a save has succeeded before browser storage confirms it.

## Polish writing guide

Documentation remains in English. All visible game UI, events, tutorials, and error messages should be written in Polish. Use direct second-person language and concise, idiomatic humor. Satirize platform incentives, audience rituals, sponsorship mismatches, and creator habits. Keep the player character's identity flexible; avoid assuming gender in default copy. Prefer invented creator names, brands, platforms, and fictional communities, including generator vocabulary. No event should require the player to know a real controversy to understand the choice.

Satire can be sharp without targeting a real private person. Avoid hate speech, slurs, doxxing, real allegations, or instructions for harassment. Potentially sensitive themes such as burnout and pile-ons should offer a path to recover, not make distress the punchline. If the creator later wants real public figures or brands, review those uses separately before writing them into the game.

## Event content at scale

The creator's target is at least 200 original events later. [The event catalogue](event-catalogue.md) defines their authoring contract, [20 sample events](sample-events.md) provide the first draft content pass, and [the event system](event-system.md) defines SQL Server storage and rolls. Review and import a small representative subset first, then expand after the game loop and import/publish workflow are tested. Track coverage for calm, middle, and high-drama bands, plus low/high viewer and money states.

Each card needs a Polish prompt, response labels, exact guaranteed costs, percentages for all possible results, a readable risk preview, and reason codes for the report. The UI may group minor outcomes to save space only when their probability and consequences remain unambiguous. For private-message stories, keep the satire on choices, impersonation, and public reaction; an age concern ends contact immediately, without romantic or sexual content involving a minor.

## Accessibility and usability

- Keyboard operation for every choice; visible focus and no keyboard traps.
- Semantic labels and field errors; no metric conveyed only by an icon or color.
- Content remains usable at 320 CSS pixels and 200% zoom; horizontal scrolling is limited to optional history detail.
- Respect reduced-motion preference. Animations may emphasize a result but must never delay control.
- Do not auto-dismiss report or event text. Use concise headings for screen readers.
- Confirmation for destructive save overwrite or new-run actions; an accidental click must not erase a career.

## Visual direction

Use a recognizable streaming dashboard language without copying a real platform's trade dress. Prioritize typography, clear numeric hierarchy, and a satirical editorial voice. Reserve bright accents for choices and meaningful changes. The first design pass should test one mobile and one desktop layout before creating decorative assets.
