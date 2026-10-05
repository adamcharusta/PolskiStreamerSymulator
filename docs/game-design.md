# Game design: a dice-driven streamer career

The creator confirmed an initial 52-week run, one week per turn, satirical Polish-internet tone, **money, viewers, and drama** as the three game statistics, no starting archetypes, event eligibility influenced by all three statistics, and percentage-based consequences for **every choice**. The player chooses **one action at the start of each week**, then may respond to an event. Run length and other numeric defaults are versioned SQL Server parameters; numeric tuning belongs in [balance](balance.md).

## Objective and setup

Build a fictional Polish streaming career for up to the configured number of weeks, initially 52. Completing the configured final week without bankruptcy ends the career; reaching the bankruptcy limit ends it immediately in defeat. Either ending shows one career score based equally on the final channel audience and net profit earned after expenses, along with final money, the drama path, and notable events. The score compares runs; it does not override the defeat result. Calm, mixed, and controversy-driven careers should produce different stories.

The player chooses a streamer name and may choose a separate channel name. The setup screen suggests a streamer name by joining two SQL Server vocabulary parts; the suggestion can be regenerated or replaced with the player's own name. Name choice has no game effect. See [streamer name generation](streamer-name-generation.md). A content theme may be chosen for flavor and event eligibility, but must not grant archetype-like starting bonuses. There are **no archetypes**.

## Persistent game state

| Field | Meaning | Rule |
| --- | --- | --- |
| Week | Current week | Starts at 1; the initial published run ends after week 52, later versions may configure another length |
| Money | Available PLN | Starts at 1,500 in the first published parameters; integer; changes only through categorized cash-flow entries; initially a balance at or below -1,000 ends the run in defeat |
| Viewers | Size of the channel's regular audience | Starts at 20 in the first published parameters; non-negative integer; there is no separate follower or average-live-viewer score |
| Drama | Public controversy level | Starts at 50 in the first published parameters; integer 0–100; low is calmer, high is more controversial |
| Run history | Completed choices, rolls, events, and effects | Stored in the browser save, not the event database |
| RNG, rules, and catalogue versions | Reproducible chance and stable algorithms, parameters, and event rules | Stored in the browser save |

Money, viewers, and drama are the only core numeric game statistics. A description such as “damaged reputation” must translate into changes to these statistics; do not silently reintroduce reputation, trust, energy, equipment level, or a moral score. Temporary flags such as an event cooldown or a one-time milestone are rule state, not extra player-facing statistics.

## Money and weekly settlement

The player has one money balance. Each change to it is recorded in one of four cash-flow categories: **sponsors, donations, subscriptions, or expenses**. Sponsors, donations, and subscriptions add money; expenses subtract it. The weekly report shows these four subtotals and the net change. They are a breakdown of money, not four additional game statistics.

Subscription revenue is settled **once every week**, including a quiet week, using the run's post-action viewers and versioned rules. There is no separate active-subscriber count in the first version. The proposed amount formula is in [balance](balance.md). Sponsor and donation income occurs through applicable weekly-action or event outcomes. Guaranteed action costs, automatic encounter costs, paid event responses, and random bills are recorded as **separate expense entries**. An outcome can create several entries, for example sponsor income plus a production expense.

The invariant is `closingMoney = openingMoney + sponsors + donations + subscriptions - expenses`, with each subtotal shown as a non-negative PLN amount. Store the underlying signed entries and their source IDs in the browser save so a report can explain a change and a retry cannot pay the same entry twice.

Drama is a **position in public perception**, not a simple health bar. Low drama can unlock calm or cooperative situations, middle drama mixed ones, and high drama scandals and provocative opportunities. Some choices can gain viewers while increasing drama or gain money while losing viewers. The game should not automatically reward the lowest or highest drama in every situation.

## Weekly loop

1. Show the week, three statistics, recent events, and the available choices.
2. The player chooses **one weekly action**. Candidate actions for the first prototype are an ordinary stream, a provocative stunt, a commercial/promotion attempt, and a quiet week. These names and the final menu are **proposals**, not confirmed rules. Every offered action shows its cost and an honest summary of possible consequences.
3. Record any guaranteed action cost as an expense, then resolve the action using recorded percentage chances and one seeded dice roll. The result can add categorized cash flows and change viewers or drama. Settle this week's subscription income once after the action. All uncertain consequences have explicit probabilities. If the balance is now at or below the bankruptcy limit, finish in defeat without checking events.
4. Check which events are eligible using the resulting money (including the weekly subscription payout), viewers, drama band, week, and any narrative flags. Roll for an encounter and show at most one event during the week. If the selected event has an **automatic encounter cost**, record it as an expense before showing the event. If this reaches the bankruptcy limit, finish in defeat without offering event responses. Other eligible events do not charge anything.
5. Otherwise, the player chooses an event response. Record any **additional response cost** as another expense, roll once against that response's weighted outcomes, apply exactly one result, show its categorized money changes and other effects, then complete the week. If the balance reaches the bankruptcy limit, finish in defeat. A week may have no event if no definition passes its encounter roll.
6. Save the completed week in the browser and show a report before the next turn. If an event is pending, save the pending state before showing its choices. If any checkpoint caused bankruptcy, save the terminal state and show the defeat recap instead of a next-turn action.

Every player decision with uncertain consequences has a probability distribution. A “safe” choice may have a high chance of a modest result, while a risky choice may offer a larger upside and a meaningful downside. The final outcome is not chosen by the UI. The exact weekly action menu and odds will be tuned through the first playable slice.

## Events and choices

Events have typed conditions, including a **calm**, **middle**, or **high-drama** band, minimum/maximum viewers, and minimum/maximum money. An event can target one band or several. The authored catalogue should contain events specifically for each band and events available across bands. Eligibility is checked before an encounter roll. Choice outcomes have separate percentage chances; seeing an event does not imply its risky result will happen.

Both cost types are optional. An automatic encounter cost is unavoidable once that event is selected and may put money below zero or trigger bankruptcy; authoring conditions can restrict an event if that would be unfair. A paid response is offered only when affordable from the **post-encounter-cost** balance, and every event that survives its encounter cost must retain at least one free, selectable response. The encounter cost and response cost are displayed separately and applied at most once, including when a pending-event request is retried.

The content target is **at least 200 distinct events**. This is a later content-production milestone, not a requirement to handwrite 200 events before implementing the engine. The initial documentation contains [20 draft examples](sample-events.md); a small, representative subset covering each drama band, viewer threshold, money threshold, and outcome shape is enough for the first playable slice. Each event needs stable IDs, Polish text, at least two meaningful responses, explicit probabilities, and effects using only the three statistics. See [event system](event-system.md) and [event catalogue](event-catalogue.md).

Example design theme: a fan sends a private message. Responses include ignoring it, replying politely, or flirting. A reply may lead to a pleasant adult interaction, an impersonator publishing the messages, or an age concern that ends contact immediately. The consequences are expressed as viewer, money, and drama changes. The story does not develop romantic or sexual content involving a minor, and impersonation is the harmful act rather than a person's gender presentation. This is a design example; exact copy, odds, and effects require content review.

## Progression and ending

The creator confirmed an **automatic defeat at a balance of -1,000 PLN or less** for the initial published parameters, including exactly -1,000 PLN. The threshold is a typed, versioned field in the shared SQL Server game catalogue; an active run keeps its published catalogue version. Check the balance after each atomic step: (1) the weekly action, its rolled outcome, and that week's subscription settlement together; (2) a selected event's automatic encounter cost; (3) the chosen response's guaranteed cost and rolled outcome together. Cash entries inside a step reconcile before the check, so their order cannot change the ending. Once defeated, no more choices, event rolls, or weekly settlements occur. If bankruptcy happens during the configured final week, defeat takes precedence over ordinary completion.

Both the configured-final-week and defeat recaps show the single career score, its audience and net-profit components, money ledger totals, drama path, and choices made. The defeat recap is clearly labelled as a loss even if its score is high. Net profit excludes that run's pinned starting balance, initially 1,500 PLN; drama has no direct score bonus or penalty. The formula and provisional reference values are in [balance](balance.md). Viewer milestones can create one-time story beats and unlock higher-reach events; their thresholds are proposed until audience growth is tuned. Do not add an invented reputation or community score.

## Information and fairness

- Show guaranteed costs, possible outcomes, and their percentages before confirmation. Do not hide a severe result behind a vague “small risk” label.
- Use a seeded, versioned random algorithm so the same state, catalogue, and choices produce the same outcome.
- Apply one outcome per choice and record the roll, outcome ID, and numeric deltas. Retries must not roll again.
- Keep losses bounded so a single unlucky event does not erase a healthy run without a clear, deliberately accepted risk.
- Let calm, mixed, and controversial paths all produce interesting events and viable endings. Human playtests decide whether those paths feel different and fair.
