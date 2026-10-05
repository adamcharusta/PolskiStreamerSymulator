# Experience, interface, and content

## Screen map

| Screen | Essential information and action |
| --- | --- |
| Start | Short premise, new game, load game, and a clear note that career saves stay in this browser and can be lost if browser data is cleared |
| Setup | Names, content focus, archetype explanation, and start confirmation |
| Dashboard / plan | Week, core metrics, latest trend, format, workload, optional action, forecast, and resolve button |
| Event choice | Situation, 2–3 choices, stated direct costs or benefits, then decision confirmation |
| Weekly report | Deltas with causes, stream performance, event result, and continue button |
| Career history | Chronological weekly reports with milestone markers and filters by year or event type if needed |
| Save/load | Named browser-local slots, timestamps, version, overwrite confirmation, and recovery guidance |
| Final recap | Career timeline, three outcome dimensions, key decisions, replay, and return to start |

The dashboard is the main screen. On narrow viewports, show the week, the four most important metrics, then decisions and the resolve button before secondary details. Keep a persistent route to history and saves. Avoid a full-screen wall of numbers.

## Feedback rules

- Every decision has a short explanation of its intended trade-off.
- A forecast distinguishes guaranteed changes (cost, base energy) from estimated changes (audience, event risk).
- Each weekly report states: what the player chose, what happened, why the key metrics moved, and what changed for the next week.
- Use both text and visual direction for increases and decreases; color alone is insufficient.
- Flag milestone unlocks at the week they occur, and show them again in the recap.
- Make saving status visible. Never imply a save has succeeded before browser storage confirms it.

## Polish writing guide

Documentation remains in English. All visible game UI, events, tutorials, and error messages should be written in Polish. Use direct second-person language and concise, idiomatic humor. Satirize platform incentives, audience rituals, sponsorship mismatches, and creator habits. Keep the player character's identity flexible; avoid assuming gender in default copy. Prefer invented creator names, brands, platforms, and fictional communities. No event should require the player to know a real controversy to understand the choice.

Satire can be sharp without targeting a real private person. Avoid hate speech, slurs, doxxing, real allegations, or instructions for harassment. Potentially sensitive themes such as burnout and pile-ons should offer a path to recover, not make distress the punchline. If the creator later wants real public figures or brands, review those uses separately before writing them into the game.

## Minimum event catalogue for MVP

The 12 initial event rules and stable IDs are in [the event catalogue](event-catalogue.md). Their SQLite representation, probability rolls, and publishing rules are in [the event system](event-system.md). The table summarizes gameplay roles; final player-facing copy will be in Polish.

| Theme | Trigger | Example trade-off |
| --- | --- | --- |
| Unexpected clip | Streamed week | Take the wave for reach or keep a smaller, more loyal audience |
| Community in-joke | Trust above 45 | Spend time nurturing it or focus on discovery |
| Audio failure | Equipment below 3 | Pay for repair or accept a weaker week |
| Viewer suggestion | Any stream | Try an unfamiliar format or stay with the plan |
| Collaboration invite | 100+ followers | Share reach and effort or decline |
| Awkward sponsor | 1,000+ followers, reputation above 40 | Take cash with trust risk or pass |
| Good-fit sponsor | 1,000+ followers, trust above 55 | Take a smaller cash reward or invest in community goodwill |
| Moderation crisis | 100+ followers | Spend energy handling it or lose trust |
| Creator spat parody | Reputation above 30 | Engage for reach with reputation risk or step away |
| Charity stream | Cash above 100 PLN | Give time and money for community benefit or postpone |
| Algorithm shake-up | Any stream | Adapt format for discovery or protect consistency |
| Creative slump | Energy below 35 | Rest and recover or push through at a cost |

Each card needs a Polish prompt, option labels, exact direct costs, risk preview, and reason codes for the report. Use the event catalogue for eligibility and effects. Do not implement an event from only the theme in this table.

## Accessibility and usability

- Keyboard operation for every choice; visible focus and no keyboard traps.
- Semantic labels and field errors; no metric conveyed only by an icon or color.
- Content remains usable at 320 CSS pixels and 200% zoom; horizontal scrolling is limited to optional history detail.
- Respect reduced-motion preference. Animations may emphasize a result but must never delay control.
- Do not auto-dismiss report or event text. Use concise headings for screen readers.
- Confirmation for destructive save overwrite or new-run actions; an accidental click must not erase a career.

## Visual direction

Use a recognizable streaming dashboard language without copying a real platform's trade dress. Prioritize typography, clear numeric hierarchy, and a satirical editorial voice. Reserve bright accents for choices and meaningful changes. The first design pass should test one mobile and one desktop layout before creating decorative assets.
