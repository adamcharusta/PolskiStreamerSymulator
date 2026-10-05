# Experience, interface, and content

## Screen map

| Screen | Essential information and action |
| --- | --- |
| Start | Short premise, Polish/English language switch, new game, load game, a clear note that career saves stay in this browser and can be lost if browser data is cleared, and access to the privacy notice explaining default aggregate statistics |
| Setup | One editable streamer-name field with a two-part SQLite suggestion and regenerate button, the pinned starting statistics and run length, and start confirmation; no channel name, content theme, platform, or archetype selection |
| Dashboard / plan | Current week out of the pinned total, money, viewers, drama, latest trend, one weekly action selection, percentage previews, and resolve button |
| Event choice | Current event in a week that may contain several, any encounter cost already charged, response-specific costs, possible outcomes with percentages, and confirmation |
| Weekly report | Weekly action and every event response, dice results, viewers/drama deltas, opening and closing money, sponsor/donation/subscription/expense subtotals, and continue button |
| Career history | Chronological weekly reports with event and follow-up links where a story continued, plus an event-type filter if needed |
| Save/load | One autosave and three named browser-local manual slots, timestamps, version, overwrite confirmation, file export/import, and recovery guidance |
| Final recap | Completion, bankruptcy defeat, or special early ending stated first, then one career score with audience and net-profit point breakdown, final money/viewers/drama, cumulative cash-flow categories, career timeline, key decisions, replay, and return to start |
| Content admin | Owner-only panel at `admin.polskistreamersymulator.pl` with GitHub sign-in, draft list, bilingual editor, validation errors, preview, and explicit publish action; never expose draft or mutation APIs to anonymous players |

The dashboard is the main screen. On narrow viewports, show the week and all three statistics before choices and secondary details. Keep a persistent route to history and saves. Avoid a full-screen wall of numbers.

## Feedback rules

- Every decision has a short explanation of its intended trade-off.
- A forecast distinguishes guaranteed costs from weighted outcomes. Show each outcome's percentage and meaningful effect before confirmation, especially for a high-drama risk.
- When an event has an automatic encounter cost, show the amount immediately as an expense already charged. Show a paid response's additional cost separately before the player selects it. A paid choice remains selectable without enough cash, even when its cost alone crosses the debt limit; its rolled outcome can rescue the run. Keep a free response available while the run remains active.
- Show the pinned bankruptcy limit, initially -1,000 PLN, near the money balance. In a choice preview, warn when a paid choice enters debt, when its cost temporarily crosses the limit, and when the final outcome may or must bankrupt the player. If an automatic encounter cost reaches the limit, show that cost and a defeat recap immediately instead of presenting response choices. Permanent ban, channel closure, and retirement must be clearly flagged among possible outcomes before the choice. The recap labels permanent ban and channel closure as defeats, and retirement as neutral. Completion, bankruptcy, and special endings use distinct labels; a high score does not change the ending type.
- Each weekly report states: what the player chose in the weekly action and each event, what happened, why the key metrics moved, and what changed for the next week. Show interim results between events without paying subscriptions again or prematurely closing the week.
- Show the four money categories even when a category is zero. Explain a negative net week through its expense entries; the sum of the categories must match the displayed money change.
- In the final recap, show the score calculation in plain language: final viewers contribute half of the scoring formula, and net profit from sponsors, donations, and subscriptions after expenses contributes the other half. Show the run's pinned starting balance (initially 1,500 PLN) separately from earned profit. Drama appears as career context, not a score component. The confirmed first formula and reference values live in [balance](balance.md).
- The suggested streamer name is visibly editable; a custom entry takes precedence. Regenerate only on request and keep the current text if the generator fails. Show an inline validation error for an invalid custom name. The detailed flow and vocabulary rules are in [streamer name generation](streamer-name-generation.md).
- Use both text and visual direction for increases and decreases; color alone is insufficient.
- When a later event continues a prior story, link the two reports in history and the recap. A result may hint that a story could return, but must not promise a follow-up that still depends on an encounter roll.
- Make saving status visible. Never imply a save has succeeded before browser storage confirms it.
- Export a complete local save to a file, including the chosen name and career history, with a clear warning that the file contains this information. Import validates file size, schema/rules/catalogue compatibility, and state before offering an overwrite confirmation for one slot; an invalid file leaves existing slots untouched. These operations do not upload a save to the server.

## Polish and English writing guide

Documentation remains in English. All visible game UI, events, tutorials, validation messages, and error messages need Polish and English versions for the first release. Polish is the confirmed default locale; the player can switch language from the start screen and during a run without changing its catalogue version, RNG state, choices, or outcomes. Save the language preference separately from the career state. Format the confirmed in-game currency as PLN in both locales.

Write direct second-person copy and concise, idiomatic humor in each language. The creator wants sharp, recognizable allusions to specific Polish internet stories while keeping personal names, brands, and platforms fictional. Adapt jokes so English-speaking players can understand the decision even when they do not recognize the allusion; do not rely on literal translations that obscure a risk or reward. Keep the player character's identity flexible; avoid assuming gender in default copy. Every choice preview must communicate the same costs, percentages, and consequences in both languages.

Before publication, review each recognizable allusion for whether it concerns a public creator and whether it accidentally identifies a private person or presents an unverified allegation as fact. Private people must not be the recognizable subjects of events. Do not include real names, handles, logos, or private contact details in the first version. Avoid hate speech, doxxing, and instructions for harassment. The creator chose no additional blanket topic exclusions; review sensitive themes case by case in both languages so the English adaptation does not change the implication of the Polish text.

## Event content at scale

The creator is considering roughly 200–400 original events for the first public release; the final count will be decided later. [The event catalogue](event-catalogue.md) defines their authoring contract, [20 sample events](sample-events.md) provide the first draft content pass, and [the event system](event-system.md) defines SQLite storage and rolls. Review a small representative subset first, then expand after the game loop and admin publishing workflow are tested. Track coverage for calm, middle, and high-drama bands, plus low/high viewer and money states.

Each card needs Polish and English prompts, response labels, result text, exact guaranteed costs, percentages for all possible results, a readable risk preview, and stable reason codes for the report. The UI may group minor outcomes to save space only when their probability and consequences remain unambiguous. Some events occur at most once per career, while others can return after a cooldown; optional story follow-ups remain subject to their own encounter rolls. For private-message stories, keep the satire on choices, impersonation, and public reaction; an age concern ends contact immediately, without romantic or sexual content involving a minor. The 20 current examples are Polish drafts and need English adaptation before publication.

## Accessibility and usability

- Keyboard operation for every choice; visible focus and no keyboard traps.
- Semantic labels and field errors; no metric conveyed only by an icon or color.
- Content remains usable at 320 CSS pixels and 200% zoom; horizontal scrolling is limited to optional history detail.
- Respect reduced-motion preference. Animations may emphasize a result but must never delay control.
- Do not auto-dismiss report or event text. Use concise headings for screen readers.
- Confirmation for destructive save overwrite or new-run actions; an accidental click must not erase a career.

## Visual direction

Use a recognizable streaming dashboard language without copying a real platform's trade dress. Prioritize typography, clear numeric hierarchy, and a satirical editorial voice. Reserve bright accents for choices and meaningful changes. The first design pass should test one mobile and one desktop layout before creating decorative assets.
