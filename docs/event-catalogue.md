# Event catalogue: authoring contract and first example

The creator is considering **roughly 200–400 original events for the first public release**; the exact count remains open. The first documentation pass now has [20 Polish draft sample events](sample-events.md) to illustrate conditions, costs, choices, and weighted outcomes; they are not approved or imported into SQLite. Published events need reviewed Polish and English copy. The former twelve-event table was based on retired energy, trust, reputation, follower, and equipment statistics and is no longer a valid implementation specification.

## Authoring rules

Every event definition needs:

1. A stable ID, Polish and English titles and setup text, and optional content tags. The ID and numeric rules are shared by both locales.
2. Typed eligibility rules for drama band, viewers, money, and week. A follow-up can additionally require a browser-local narrative flag set by a specific earlier result and a bounded window of later weeks. An event can be calm-only, middle-only, high-drama-only, or available across bands.
3. An encounter chance, a per-event repeat policy (`once` or repeatable after a cooldown), and optional **automatic encounter cost**. Eligibility is checked before the encounter roll, including for follow-ups; successful events compete under the same seeded selection rule. A week may present multiple distinct events up to the pinned cap. Only selected events charge their encounter costs.
4. At least two responses with Polish and English labels and honest previews. Each response has at least two meaningfully distinct weighted outcomes; weights sum to 100%. An optional response cost is shown separately from the already charged encounter cost. If the encounter cost has not ended the run at the bankruptcy limit, at least one response remains free and selectable.
5. Polish and English result text and effects using **money, viewers, and drama only** as numeric statistics. Any money effect must be a categorized sponsor/donation/expense entry; the weekly subscription payout is handled once by the weekly rules. A result may move more than one statistic, set a narrative flag, or carry an approved special terminal reason such as `permanent_ban`. Severe outcomes, especially early endings, need an understandable warning and a safer alternative in both languages.
6. Review both locales for originality, grammar, gender-flexible player copy, equivalent risk disclosure, recognizable real-story allusions about public creators only, and consistency with [experience and content](experience-and-content.md). Keep names, handles, brands, and platforms fictional; do not make private people identifiable subjects. A missing translation blocks publication.
7. If an outcome starts a story chain, name the flag it sets and the later event or alternative events that can consume it. The later event retains its own encounter chance, may never appear, and must not be promised as certain in player copy.

Event conditions and outcomes are stored in SQLite as described in [event system](event-system.md). A published version is immutable so an active browser save can finish against the same rules.

## Design example: a private message from a fan

This example captures the creator's intended shape of an event. The numbers below are **illustrative tuning values**, not approved final content. Final Polish and English copy will be written during content production.

| Field | Example |
| --- | --- |
| ID | `fan_private_message` |
| Eligibility | Viewers at least 20; any drama band; no same-event encounter in the last four weeks |
| Encounter chance | 5% on an eligible week |
| Automatic encounter cost | 0 PLN for this example; other events may have one |
| Response 1 | Ignore the message |
| Response 2 | Reply politely without flirting |
| Response 3 | Flirt, accepting a clearly displayed higher risk |

| Response | Illustrative outcome distribution | Effects |
| --- | --- | --- |
| Ignore | 90% nothing develops; 10% the fan comments publicly on being ignored | `0` or `-2 viewers, +1 drama` |
| Reply politely | 70% pleasant exchange with a verified adult; 20% no lasting effect; 10% screenshots appear out of context | `+8 viewers, -1 drama`; `0`; or `+5 drama` |
| Flirt | 55% pleasant exchange with a verified adult; 25% an impersonator leaks the chat; 20% age cannot be verified and contact ends immediately | `+12 viewers, -2 drama`; `-8 viewers, +20 drama`; or `-15 viewers, +30 drama` |

The event does not develop romantic or sexual content involving a minor. The impersonation outcome treats deception and publishing private messages as the problem; it does not make gender presentation the punchline. “Reputation loss” in story language maps to viewer and drama deltas because there is no reputation statistic.

For a cost-bearing example, a cancelled venue could charge a **60 PLN encounter expense** when selected. The player could then choose a free home-stream response or a replacement venue response with an **additional 40 PLN guaranteed expense**, even if the cost temporarily takes the balance below the pinned bankruptcy limit. The response still rolls for its outcome; if the paid response wins 150 PLN from a sponsor, that result creates a separate sponsor-income entry before the bankruptcy check. The three entries remain distinct in the report. These amounts and story details are illustrative, not published content.

## Build the catalogue in stages

- **Engine fixture:** a few events spanning calm, middle, and high drama, including viewer and money thresholds, a debt-funded option near the bankruptcy limit, a special terminal outcome, a week with two events, and two- and three-outcome responses.
- **First playable content set:** start from the 20 draft examples in [sample events](sample-events.md), review and enter a representative subset through the admin workflow, then expand if a 52-week test run repeats situations too often. Twenty examples are a documentation milestone, not a promise that 20 published events provide enough variety.
- **First public release target under consideration:** roughly 200–400 reviewed events, distributed across drama bands and audience/wealth stages, including both one-time and repeatable events and some optional chains. Set the exact count after content-production estimates and playtests. Track eligibility and encounter counts so adding events does not unintentionally make one band much busier than another. Tune the published encounter odds toward an event in most non-terminal weeks without guaranteeing one every week.

No event in this document is automatically approved for publication; final odds, deltas, copy, and content boundaries need review and seeded balance tests.
