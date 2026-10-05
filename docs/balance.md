# Balance model: three statistics and weighted outcomes

The creator confirmed the three-statistic model, initial starting values, bankruptcy threshold, and dice-driven decisions. Exact odds, viewer growth, and income remain **provisional** until simulation and playtests. The numeric values below are the **first published defaults in SQL Server `GameParameters`**; changing them later creates a new catalogue version for new runs. The Domain code owns the formulas and invariants. All chance weights use basis points (`10,000 = 100%`) and sum to 10,000 for each choice.

## Starting values and bounds

| Statistic | Start | Bound / interpretation |
| --- | ---: | --- |
| Money | 1,500 PLN | Integer; debt is allowed above -1,000 PLN; at or below -1,000 PLN the run ends in defeat |
| Viewers | 20 | Non-negative integer; the channel's regular audience |
| Drama | 50 | Integer 0–100; 50 is neutral |

The first `RunLengthWeeks` is **52**. The server and UI read it from the run's pinned parameters; they do not treat week 52 as a permanent code constant. The first starting money, viewers, and drama values are likewise read from that row. The [SQL Server game catalogue](event-system.md) lists every typed configurable field and its validation.

The previous draft's one average live viewer, energy, trust, reputation, and equipment level are retired. Do not implement them as hidden replacement statistics. For first-pass event eligibility, **provisional** bands are 0–33 calm, 34–66 middle, and 67–100 high drama; the first two upper boundaries come from the pinned `GameParameters` row. An event can target one or more bands and add money/viewer requirements. Clamp viewers at zero and drama to 0–100 after each completed outcome. Money has no arbitrary upper cap.

## Bankruptcy limit

The initial `BankruptcyThresholdPln` is **-1,000**. Under that first published version, a run remains active at -999 PLN and ends in defeat at -1,000 PLN or any lower balance. This is a signed, typed field in `GameParameters`, not a player balance or an event definition. Active runs keep the value associated with their pinned catalogue version. The exact settlement checkpoints and loss priority are in [game design](game-design.md).

Treat the weekly action, its rolled outcome, and the regular subscription settlement as one atomic balance check. Then check again after a selected event's automatic encounter cost, and after a response's cost plus rolled outcome. A threshold crossing inside a multi-entry atomic step does not cause defeat if the step's reconciled closing balance recovers above the limit. Once a checkpoint ends the run, later steps do not occur. Preserve all completed ledger entries in the defeat recap.

## Percentage-roll contract

1. Validate that a weekly action or event response is available and its guaranteed money cost is affordable when selected. An automatic event encounter cost is unavoidable after that event is selected and may create debt.
2. Record any stated weekly-action or response cost as an expense and subtract it once before that choice's outcome roll. A cost is not a random consequence.
3. Read the choice's immutable, ordered outcomes. Their `ChanceBps` values must sum to exactly 10,000; each value is an integer from 0 to 10,000.
4. Consume one value in `[0, 9999]` from the saved seeded PRNG. Select the outcome whose cumulative interval contains that value. Apply its categorized cash-flow entries and viewer/drama deltas once.
5. Record the pre-choice state, choice ID, roll, outcome ID, cash-flow entries, and actual deltas in the report and browser save. A retry with the same state and choice returns the same result.

## Cash-flow accounting

| Category | Direction | Source |
| --- | --- | --- |
| Sponsors | Income | Sponsor deal from a weekly action or event outcome |
| Donations | Income | Viewer donation from a weekly action or event outcome |
| Subscriptions | Income | One weekly settlement, including quiet weeks |
| Expenses | Outflow | Guaranteed choice cost, operating bill, or event loss |

Each cash-flow entry has a category, positive magnitude in whole PLN, signed effect on money, week, and source action/event/outcome ID. A result may contain several entries. No outcome can mutate money without one of these entries. Subtotals are non-negative; expenses are subtracted when calculating net money.

Illustration: opening money 1,500 PLN, sponsors 200 PLN, donations 35 PLN, subscriptions 18 PLN, and expenses 60 PLN yields closing money **1,693 PLN**. These numbers are a ledger example, not approved payouts.

Settle subscription revenue once per week **after** the weekly action and **before** event eligibility. The proposed first formula is deliberately simple and deterministic:

```text
weeklySubscriptionsPln = floor(max(0, postActionViewers) / SubscriptionViewersPerPln)
```

| Post-action viewers | Weekly subscriptions |
| ---: | ---: |
| 0 | 0 PLN |
| 20 (starting audience) | 2 PLN |
| 100 | 10 PLN |
| 250 | 25 PLN |
| 1,000 | 100 PLN |

The first published `SubscriptionViewersPerPln` is **10**, which produces the example table. Record that amount once as a subscription-income ledger entry, including on a quiet week. It is an abstract audience-based payout, not a separately simulated number of paying subscribers. There is no extra subscription dice roll or direct drama multiplier; weekly choices and events already affect future payments through viewers. An event later in the same week cannot retroactively change the amount just settled. Pin the divisor through the catalogue version and the formula algorithm through the code rules version; tune the divisor against full-run income and expense distributions. This formula and its first divisor are **proposed**, not creator-confirmed.

When an event is selected after its encounter rolls, record any `EncounterCostPln` as one expense **before** presenting responses and save that paid state. Charge no cost for merely eligible or unselected events. Assess response affordability after this deduction; the event must still offer a free response. A selected response may have its own `GuaranteedCostPln`, charged once before its outcome roll. An outcome may add a further expense if that is the rolled consequence. These three expense sources need distinct IDs in the saved ledger so retries cannot double-charge them.

Example only: a cautious response could have 80% for a small positive outcome and 20% for no change; a risky response could have 25% for a large viewer gain, 50% for a modest result, and 25% for a drama spike. These percentages are illustrations, **not approved event data**. Serious negative results need a clear risk preview and an alternative response.

An event's **encounter chance** is distinct from each response's **outcome chance**. Check eligibility first, roll the encounter chance for eligible events in stable ID order, select at most one passed event, then roll only the response the player actually chooses. The exact overall encounter frequency is a tuning target, not a fixed “one event per four weeks” rule.

## Final score

The **single career score** uses only the channel audience at the end of the run and the **net profit earned during that run**. Drama has no direct score term; it can still influence which events appear and therefore change audience or profit indirectly. The first 1,500 PLN starting balance is capital, not earnings. Calculate profit from the full ledger, including every guaranteed cost and rolled expense, using the pinned starting balance:

```text
netProfitPln = totalSponsors + totalDonations + totalSubscriptions - totalExpenses
             = finalMoneyPln - StartingMoneyPln
```

Negative profit is allowed and lowers the score. The creator confirmed equal **50% / 50% weighting** for audience and profit. To put viewers and PLN on comparable scales without a hard point ceiling, use the following **provisional scoring curve**. Its initial SQL Server references are 1,000 viewers, 5,000 PLN net profit, and 500 points per component. Verify these values through 52-week simulations. Pin the values to the catalogue version and the formula algorithm to the code rules version.

```text
audiencePoints = roundAwayFromZero(ScorePointsPerComponent × sqrt(finalViewers / ScoreAudienceReferenceViewers))
profitPoints   = sign(netProfitPln) × roundAwayFromZero(ScorePointsPerComponent × sqrt(abs(netProfitPln) / ScoreProfitReferencePln))
careerScore    = max(0, audiencePoints + profitPoints)
```

At the first two reference values, each component contributes 500 points and the result is 1,000. Points can exceed 1,000; there is no score cap that would make later gains worthless. Square roots preserve a positive marginal value for viewers and PLN before integer rounding while reducing the effect of very large outliers. Compute the rounded components once in `Domain` and add those same integers in the recap; do not separately round a displayed total. `finalViewers` is the current channel audience when the run ends, either after its configured final week or at bankruptcy, not its peak or average. Apply the same formula to a defeat recap without a time bonus; its score does not turn defeat into a win. A zero-profit run receives zero profit points; a loss receives negative profit points, and the displayed total cannot drop below zero.

| Final viewers | Net profit | Audience points | Profit points | Career score |
| ---: | ---: | ---: | ---: | ---: |
| 20 | 0 PLN | 71 | 0 | 71 |
| 250 | 1,250 PLN | 250 | 250 | 500 |
| 1,000 | 5,000 PLN | 500 | 500 | 1,000 |
| 250 | -1,250 PLN | 250 | -250 | 0 |

The score is a comparison and replay incentive, not a required win threshold. Review its two reference values after balance sweeps: if typical completed careers gain far more points from one component, adjust the references together and version the change. Do not add drama, peak audience, number of events, or a hidden moral bonus to the score.

## Weekly balance to decide in the first slice

The prior draft used formula-based follower growth, viewer estimates, energy and reputation. Those formulas are retired. Each week the player selects **one action** with an explicit outcome table affecting only money, viewers, and drama. Candidate first-prototype actions are an ordinary stream, a provocative stunt, a commercial/promotion attempt, and a quiet week; their final names, odds, costs, and effects remain open. Once authored, the action definitions and weighted outcomes live in the same published SQL Server catalogue as events and numeric parameters. Do not implement the old quality/income formulas by renaming their outputs.

For early simulations, track at least:

- Distribution of viewers and money at weeks 1, 13, 26, and 52 for the first configuration; use equivalent checkpoints if run length changes.
- Distribution of final audience points, profit points, and career scores; check whether the two components have comparable influence in typical completed runs.
- Fraction of turns and runs spent in each drama band; paths must be able to move between bands.
- Expected value and worst ordinary loss for each choice; no option should dominate all three statistics.
- Frequency of eligible, encountered, and repeated events by drama band.
- Percentage of runs reaching week 52 versus bankrupting, plus the week and expense source of each defeat. Check whether unavoidable encounter costs create unfair losses.

Tune exact odds and deltas against these results and human playtests. Keep the same seeded scenarios when comparing revisions.
