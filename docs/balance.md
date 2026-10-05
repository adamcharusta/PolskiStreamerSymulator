# Balance model: initial tuning hypothesis

These numbers are **provisional**, intended to make an MVP implementable and testable. They are not claims about real streaming platforms or real income. Change them after simulation and playtests, then update this document.

## Initial state and bounds

| Metric | Default | Bounds |
| --- | ---: | ---: |
| Followers | 20 | 0 or more |
| Cash | 1,500 PLN | No upper cap; debt floor at -500 PLN |
| Energy | 75 | 0–100 |
| Community trust | 50 | 0–100 |
| Reputation | 50 | 0–100 |
| Equipment | 1 | 1–5 |
| Average viewers | 1 | 0 or more |

Archetype modifiers should be small: no more than 10% on a formula or 5 points on a starting 0–100 metric. Start screen previews the actual modifiers. This avoids an archetype determining the outcome by itself.

## Workload and action table

| Workload | Live hours | Base quality modifier | Energy change before event |
| --- | ---: | ---: | ---: |
| Break | 0 | No content | +18 |
| Regular | 8 | 1.00 | -5 |
| Intensive | 20 | 1.25 if starting energy ≥30; 0.50 otherwise | -18 |

| Optional action | Cost | Immediate effect |
| --- | ---: | --- |
| None | 0 PLN | None |
| Community outreach | 0 PLN | +3 trust, -3 energy |
| Promotion | 80 PLN | +20% discovery for this week's content |
| Equipment upgrade | `250 × current equipment level` PLN | +1 equipment level, maximum 5; no same-week quality bonus |

Upgrade and promotion require enough cash **at planning time**. Community outreach is available during a break. A break cannot include promotion. Weekly fixed costs are 15 PLN, including break weeks.

## Resolution order and formulas

Use a seeded PRNG. Round final integer metrics once per stage, not after each multiplier. Clamp bounded metrics after all changes within a stage. Use this order:

1. Validate the input and pay optional-action cost.
2. Apply workload and action energy/trust changes, but calculate this week's content quality using **energy at the start of the week**.
3. If streaming, compute `quality = clamp(0.2, 1.2, 0.2 + startEnergy / 100 + 0.05 × (equipment - 1)) × workloadModifier × archetypeModifier × formatModifier`. The intensive-workload modifier falls to 0.50 below 30 starting energy, so breaks matter.
4. Compute discovery `base = (4 + sqrt(startFollowers) × 1.5) × quality × promotionModifier`; multiply by a seeded noise factor in `[0.85, 1.15]` and apply the soft ceiling below. The resulting new followers are `max(0, round(base))`. A break has `newFollowers = 0` and loses `max(0, round(startFollowers × 0.005))` followers.
5. Estimate average viewers as `max(0, round(endFollowers × (0.02 + trust / 2500) × quality))`. On a break, average viewers are 0. This is the weekly stream metric, not a persistent moving average.
6. Compute baseline income as `round(averageViewers × liveHours × 0.12)` PLN. Apply sponsor or event cash separately. Subtract the 15 PLN weekly fixed cost.
7. Apply format side effects. Check milestones using this week's new follower total, then resolve at most one event and its chosen outcome. Apply event metric deltas, clamp energy/trust/reputation, then record the full report.
8. Update the consecutive-debt counter using the resulting cash. End the run if it reaches four weeks with cash below -500 PLN. Otherwise advance to the next week or annual recap.

`formatModifier` starts at 1.00 for gaming, 0.95 for just chatting, 1.15 for challenge/IRL, and 0.90 for tutorial/commentary. Format side effects after a streamed week: gaming +1 trust; just chatting +2 trust and -1 reputation when starting energy is below 30; challenge/IRL -3 extra energy; tutorial/commentary +1 reputation. These are starting values, not realism claims.

There is a **soft discovery ceiling**: above 10,000 followers, divide new followers by `1 + (startFollowers - 10000) / 10000`. The ceiling slows runaway growth while leaving milestone events meaningful. Events may exceed the ordinary weekly gain but should be bounded to at most 10% of current followers plus 100 followers in one week.

## Random events

- Every week, first check deterministic milestone events. If none takes the event slot, roll once for **each eligible, off-cooldown event** using its `OccurrenceChanceBps` from the published SQLite catalogue. If several pass, select one uniformly. Show at most one event total. See [event system](event-system.md).
- Random events use their own eligibility and a 4-week cooldown per event ID in the initial catalogue. If no event passes its roll, the week remains ordinary. The first-week aggregate chance is targeted at roughly one event in four weeks, but the exact rate varies with eligibility and catalogue content.
- A standard event outcome should stay within ±8 energy, ±5 trust, ±5 reputation, and ±150 PLN. Exceptional sponsor or milestone events may exceed cash by a documented amount.
- Store the event ID, roll-relevant seed state, selected option, and resulting deltas in the career log.
- A format must have at least one positive and one negative eligible event. No event may permanently remove a core action in the MVP.

## Score and tuning targets

The recap displays raw numbers plus three 0–100 scores. Proposed first formulas: reach = `round(100 × log(1 + followers) / log(10001))`, capped at 100; sustainability = `clamp(0, 100, round(50 + (endCash - startCash) / 50 - 10 × totalDebtWeeks))`; community = `round((trust + reputation) / 2)`. These are presentation summaries, not hidden modifiers to play. Tune the curves after playtests without changing the recorded raw metrics.

Initial balance targets for 1,000 seeded automated runs per common strategy:

- Median 52-week run reaches at least 100 followers without requiring lucky events.
- A regular-workload strategy can finish the year without debt if the player avoids expensive upgrades.
- Intensive workload grows faster early but has worse energy unless the player schedules breaks.
- No single archetype, format, or optional action wins all three recap dimensions across most seeds.
- Early bankruptcy is possible through sustained overspending but rare under ordinary play.

If these targets fail, change the smallest relevant constants and rerun the same seeds. Also conduct human playtests: numeric variety alone does not prove that choices feel meaningful.
