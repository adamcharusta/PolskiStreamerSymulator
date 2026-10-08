using System.Globalization;
using System.Text;
using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Engine;
using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Randomness;
using PolskiStreamerSymulatorApp.Domain.Runs;
using PolskiStreamerSymulatorApp.Domain.Tests.Runs;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Balance;

/// <summary>
/// A fixed way of choosing each week's action. The policy generator is separate from the run's generator,
/// so a strategy never consumes gameplay rolls.
/// </summary>
internal sealed record SweepStrategy(string Name, Func<ValidatedCatalog, Pcg32, string> ChooseAction);

/// <summary>
/// The statistics one strategy produced over many seeded careers.
/// </summary>
internal sealed class StrategyResult(string name)
{
    public string Name { get; } = name;

    public List<ValidatedRunState> FinalRuns { get; } = [];

    public List<int> BankruptcyWeeks { get; } = [];

    public Dictionary<int, List<long>> MoneyAtCheckpoint { get; } = [];

    public Dictionary<int, List<int>> ViewersAtCheckpoint { get; } = [];

    public int[] DramaBandWeeks { get; } = new int[3];

    public long[] CategoryTotalsPln { get; } = new long[4];

    public int PlayedWeeks { get; set; }
}

/// <summary>
/// Plays action-only careers for several strategies and renders the balance report. Numbers come from careers without events.
/// </summary>
internal static class ActionOnlySweep
{
    public const int SeedsPerStrategy = 1_000;
    public const ulong GameplayStream = 1;
    public const ulong PolicyStream = 2;

    public static readonly int[] CheckpointWeeks = [1, 13, 26, 52];

    public static IReadOnlyList<SweepStrategy> Strategies(ValidatedCatalog catalog)
    {
        List<SweepStrategy> strategies =
        [
            .. catalog.WeeklyActions.Select(static action => new SweepStrategy($"always {action.ActionId}", (_, _) => action.ActionId)),
        ];
        strategies.Add(new SweepStrategy(
            "uniform random",
            static (catalog, policy) => catalog.WeeklyActions[(int)policy.NextBounded((uint)catalog.WeeklyActions.Count)].ActionId));
        return strategies;
    }

    public static StrategyResult Run(ValidatedCatalog catalog, SweepStrategy strategy, int seeds)
    {
        StrategyResult result = new(strategy.Name);
        foreach (int week in CheckpointWeeks)
        {
            result.MoneyAtCheckpoint[week] = [];
            result.ViewersAtCheckpoint[week] = [];
        }

        for (ulong seed = 0; seed < (ulong)seeds; seed++)
        {
            RunStateValidation start = RunStarter.Start(catalog, RunStateFixtures.RunId, "Sweep", seed, GameplayStream);
            if (!start.IsValid)
            {
                throw new InvalidOperationException(string.Join(", ", start.Errors));
            }

            ValidatedRunState run = start.Value;
            Pcg32 policy = new(Pcg32.Seed(seed, PolicyStream));
            while (run.State.Status == RunStatus.Active)
            {
                WeekPlanResult planned = WeekEngine.PlanWeek(run, catalog, strategy.ChooseAction(catalog, policy));
                if (!planned.IsSuccess)
                {
                    throw new InvalidOperationException(planned.ErrorCode);
                }

                run = planned.Value;
                Record(result, catalog.Parameters, run.State);
            }

            result.FinalRuns.Add(run);
        }

        return result;
    }

    public static string Render(ValidatedCatalog catalog, IReadOnlyList<StrategyResult> results)
    {
        StringBuilder report = new();
        report.AppendLine("# Action-only balance sweep");
        report.AppendLine();
        report.AppendLine(Invariant(
            $"Careers made only of weekly actions, without events, so these numbers are no balance verdict. Catalogue version {catalog.Version}, {SeedsPerStrategy:N0} seeds per strategy, run length {catalog.Parameters.RunLengthWeeks} weeks. Money and viewer percentiles count only the runs still playing, or just completed, at that week."));
        foreach (StrategyResult result in results)
        {
            int runs = result.FinalRuns.Count;
            int bankrupt = result.BankruptcyWeeks.Count;
            string medianWeek = bankrupt == 0 ? "none" : Percentile(result.BankruptcyWeeks.ConvertAll(static week => (long)week), 50).ToString(CultureInfo.InvariantCulture);
            int played = Math.Max(1, result.PlayedWeeks);
            report.AppendLine();
            report.AppendLine(Invariant($"## {result.Name}"));
            report.AppendLine();
            report.AppendLine(Invariant($"- Bankrupt runs: {100.0 * bankrupt / runs:F1}% (median bankruptcy week: {medianWeek})"));
            report.AppendLine(Invariant(
                $"- Drama band of played weeks: calm {100.0 * result.DramaBandWeeks[0] / played:F1}%, middle {100.0 * result.DramaBandWeeks[1] / played:F1}%, high {100.0 * result.DramaBandWeeks[2] / played:F1}%"));
            report.AppendLine(Invariant(
                $"- Average per played week: sponsors {(double)result.CategoryTotalsPln[0] / played:F1} PLN, donations {(double)result.CategoryTotalsPln[1] / played:F1} PLN, subscriptions {(double)result.CategoryTotalsPln[2] / played:F1} PLN, expenses {(double)result.CategoryTotalsPln[3] / played:F1} PLN"));
            report.AppendLine();
            report.AppendLine("| Week | Runs | Money p10 | Money p50 | Money p90 | Viewers p10 | Viewers p50 | Viewers p90 |");
            report.AppendLine("| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
            foreach (int week in CheckpointWeeks)
            {
                List<long> money = result.MoneyAtCheckpoint[week];
                List<long> viewers = result.ViewersAtCheckpoint[week].ConvertAll(static count => (long)count);
                report.AppendLine(money.Count == 0
                    ? Invariant($"| {week} | 0 | - | - | - | - | - | - |")
                    : Invariant(
                        $"| {week} | {money.Count} | {Percentile(money, 10)} | {Percentile(money, 50)} | {Percentile(money, 90)} | {Percentile(viewers, 10)} | {Percentile(viewers, 50)} | {Percentile(viewers, 90)} |"));
            }
        }

        return report.ToString();
    }

    private static void Record(StrategyResult result, GameParameters parameters, RunState state)
    {
        WeekRecord played = state.History[^1];
        result.PlayedWeeks++;
        int band = state.Drama <= parameters.CalmDramaMax ? 0 : state.Drama <= parameters.MiddleDramaMax ? 1 : 2;
        result.DramaBandWeeks[band]++;
        foreach (CashFlowEntry entry in state.Ledger.Where(entry => entry.Week == played.Week))
        {
            result.CategoryTotalsPln[(int)entry.Category] += entry.AmountPln;
        }

        if (state.Status == RunStatus.Bankrupt)
        {
            result.BankruptcyWeeks.Add(played.Week);
            return;
        }

        if (result.MoneyAtCheckpoint.TryGetValue(played.Week, out List<long>? money))
        {
            money.Add(state.MoneyPln);
            result.ViewersAtCheckpoint[played.Week].Add(state.Viewers);
        }
    }

    /// <summary>Nearest-rank percentile of a non-empty list.</summary>
    private static long Percentile(List<long> values, int percent)
    {
        long[] sorted = [.. values.Order()];
        int rank = (int)Math.Ceiling(percent / 100.0 * sorted.Length);
        return sorted[Math.Max(rank, 1) - 1];
    }

    private static string Invariant(FormattableString text)
    {
        return text.ToString(CultureInfo.InvariantCulture);
    }
}
