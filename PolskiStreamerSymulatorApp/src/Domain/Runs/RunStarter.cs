using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Identity;
using PolskiStreamerSymulatorApp.Domain.Randomness;
using PolskiStreamerSymulatorApp.Domain.Versioning;

namespace PolskiStreamerSymulatorApp.Domain.Runs;

/// <summary>
/// Builds the first state of a new career from a validated catalogue. The caller supplies the run ID and the generator seed and stream.
/// </summary>
public static class RunStarter
{
    /// <summary>
    /// Returns the validated week-1 state, or the run-state validation errors for an invalid name or an empty run ID.
    /// </summary>
    public static RunStateValidation Start(ValidatedCatalog catalog, Guid runId, string streamerName, ulong seed, ulong stream)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(streamerName);

        GameParameters parameters = catalog.Parameters;
        string name = StreamerName.TryNormalize(streamerName, out string normalized) ? normalized : streamerName;
        RunState state = new(
            RulesVersion: RulesVersion.Current,
            CatalogVersion: catalog.Version,
            RunId: runId,
            StreamerName: name,
            Week: 1,
            Status: RunStatus.Active,
            MoneyPln: parameters.StartingMoneyPln,
            Viewers: parameters.StartingViewers,
            Drama: parameters.StartingDrama,
            Rng: Pcg32.Seed(seed, stream),
            Ledger: [],
            History: [],
            CurrentWeek: null,
            Flags: [],
            Ending: null);
        return RunStateValidator.Validate(state, parameters);
    }
}
