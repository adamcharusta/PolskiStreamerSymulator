using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Runs;
using PolskiStreamerSymulatorApp.Domain.Tests.Catalog;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Balance;

public sealed class ActionOnlySweepTests
{
    /// <summary>
    /// Runs only on demand: dotnet test --project tests/Domain.Tests/Domain.Tests.csproj --explicit only
    /// </summary>
    [Fact(Explicit = true)]
    public void WritesTheActionOnlyBalanceReport()
    {
        ValidatedCatalog catalog = DraftCatalog.Validated();
        List<StrategyResult> results = [];
        foreach (SweepStrategy strategy in ActionOnlySweep.Strategies(catalog))
        {
            StrategyResult result = ActionOnlySweep.Run(catalog, strategy, ActionOnlySweep.SeedsPerStrategy);
            Assert.Equal(ActionOnlySweep.SeedsPerStrategy, result.FinalRuns.Count);
            Assert.All(result.FinalRuns, run =>
            {
                Assert.NotEqual(RunStatus.Active, run.State.Status);
                RunStateValidation validation = RunStateValidator.Validate(run.State, catalog.Parameters);
                Assert.True(validation.IsValid, string.Join(", ", validation.Errors));
            });
            results.Add(result);
        }

        string report = ActionOnlySweep.Render(catalog, results);
        string path = ReportPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, report);
        TestContext.Current.TestOutputHelper?.WriteLine(report);
        TestContext.Current.TestOutputHelper?.WriteLine($"Report written to {path}");
    }

    /// <summary>The report goes to artifacts/balance/, found by walking up from the test's output folder.</summary>
    private static string ReportPath()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !string.Equals(directory.Name, "artifacts", StringComparison.OrdinalIgnoreCase))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory?.FullName ?? AppContext.BaseDirectory, "balance", "actions-only.md");
    }
}
