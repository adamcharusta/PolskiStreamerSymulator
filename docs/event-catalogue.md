# MVP event catalogue: rules specification

This is the initial rules catalogue for 12 events. All titles and descriptions below are **English design labels**, not final player-facing copy. Write original Polish prompts, option labels, and report text during content production. The numeric effects and per-event occurrence chances are tuning hypotheses governed by [balance](balance.md) and the [SQLite event system](event-system.md).

## Shared event rules

- Roll for events only after baseline weekly results and format effects. A milestone notification takes precedence over a random event that week.
- Each eligible event makes its own occurrence roll using the chance in the table and has a 4-week cooldown after it appears. If several pass, choose one uniformly using the saved PRNG state.
- A trigger referring to followers, trust, reputation, cash, equipment, or energy uses the state **after baseline resolution and before event effects**.
- Apply the selected option once, clamp followers at zero and bounded stats to 0–100, then record event ID, option ID, and numeric deltas. Cash can be negative under the debt rules.
- `+N followers` means an additive change, capped at `min(N, round(0.1 × followers + 100))`; losses cannot reduce followers below zero. A cost requiring cash is selectable only when the pre-event cash is at least that cost.
- Every event must always have at least two selectable options. If an option becomes unaffordable, provide the listed free alternative.

| ID | Event and eligibility | Occurrence chance | Option A | Option B |
| --- | --- | ---: | --- | --- |
| `clip_spread` | Unexpected clip; any streamed week | 5% | Share it widely: 70% `+60 followers, -2 trust`; 30% `+10 followers, -2 trust` | Let it circulate naturally: +25 followers, +2 trust |
| `community_joke` | Community in-joke; streamed week, trust ≥45 | 5% | Make it a recurring segment: +4 trust, -3 energy | Keep the show broad: +30 followers, -1 trust |
| `audio_fault` | Audio failure; streamed week, equipment <3, cash ≥100 PLN | 4% | Repair it: -100 PLN, +1 reputation | Work around it live: -2 reputation, +2 trust |
| `viewer_suggestion` | Viewer suggestion; any streamed week | 5% | Try it: +35 followers, -4 energy | Stay with the plan: +2 trust, +1 reputation |
| `collaboration_invite` | Collaboration; streamed week, followers ≥100 | 3% | Join: +70 followers, -5 energy | Decline politely: +2 reputation, +2 energy |
| `awkward_sponsor` | Mismatched sponsor; streamed week, followers ≥1,000, reputation >40 | 2% | Accept: +300 PLN, -5 trust, -2 reputation | Decline: +3 trust, +1 reputation |
| `good_sponsor` | Suitable sponsor; streamed week, followers ≥1,000, trust >55 | 2% | Accept: +200 PLN, -1 energy | Ask for a community perk: +100 PLN, +3 trust |
| `moderation_issue` | Chat moderation problem; streamed week, followers ≥100 | 4% | Address it: -5 energy, +3 trust | Ignore it: -4 trust, -2 reputation |
| `creator_spat` | Creator spat parody; streamed week, reputation >30 | 3% | Reply publicly: +50 followers, -4 reputation | Step away: +2 reputation, +2 energy |
| `charity_invite` | Charity opportunity; any week, cash ≥100 PLN | 3% | Contribute: -100 PLN, +4 trust, +2 reputation | Postpone: no direct metric change |
| `algorithm_shift` | Discovery changes; any streamed week | 4% | Adapt this week: +40 followers, -5 energy | Keep the channel familiar: +2 trust, -15 followers |
| `creative_slump` | Creative slump; energy <35 | 5% | Lighten this week: +8 energy, -10 followers | Push through: +20 followers, -6 energy |

The `creative_slump` event's first option is immediate recovery in the MVP; it does not silently change the following week's selected workload. The `charity_invite` can appear on a break week and must describe an off-stream contribution.

## Milestone notifications

Crossing 100, 1,000, or 10,000 followers produces a one-time notification and badge. It has no numeric effect. Store the unlocked milestone ID so a later drop in followers does not repeat it. If baseline growth crosses a milestone, show the notification instead of drawing a random event. If a random event pushes the total over a milestone, include the badge in that event's report without showing a second event card. If multiple milestones are crossed at once, show the highest one and record all crossed IDs in the weekly report.

## Content acceptance criteria

Every card needs concise Polish setup text, option labels, exact cost text, risk preview, and two result variants that match the rules. Review satire for clarity and for the boundaries in `experience-and-content.md`. Ensure the same event reads sensibly for every eligible content format and for a character of any gender.
