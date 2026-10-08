using PolskiStreamerSymulatorApp.Domain.Runs;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Runs;

/// <summary>
/// Structural equality for run states. Records compare their list members by reference, so lists are compared element by element.
/// </summary>
internal static class RunStateAssert
{
    public static void Equivalent(RunState expected, RunState actual)
    {
        Assert.Equal(Scalars(expected), Scalars(actual));
        Assert.Equal(expected.Ledger, actual.Ledger);
        Assert.Equal(expected.Flags, actual.Flags);
        Assert.Equal(expected.History.Count, actual.History.Count);
        for (int index = 0; index < expected.History.Count; index++)
        {
            Week(expected.History[index], actual.History[index]);
        }

        if (expected.CurrentWeek is null || actual.CurrentWeek is null)
        {
            Assert.Equal(expected.CurrentWeek is null, actual.CurrentWeek is null);
            return;
        }

        Assert.Equal(
            (expected.CurrentWeek.Week, expected.CurrentWeek.Action, expected.CurrentWeek.Pending),
            (actual.CurrentWeek.Week, actual.CurrentWeek.Action, actual.CurrentWeek.Pending));
        Assert.Equal(expected.CurrentWeek.EncounterRolls, actual.CurrentWeek.EncounterRolls);
        Events(expected.CurrentWeek.ResolvedEvents, actual.CurrentWeek.ResolvedEvents);
    }

    private static object Scalars(RunState state)
    {
        return (
            state.RulesVersion,
            state.CatalogVersion,
            state.RunId,
            state.StreamerName,
            state.Week,
            state.Status,
            state.MoneyPln,
            state.Viewers,
            state.Drama,
            state.Rng,
            state.Ending);
    }

    private static object ResponseScalars(ResponseResolution response)
    {
        return (response.OptionId, response.Roll, response.OutcomeId, response.ViewersDelta, response.DramaDelta, response.TerminalReasonCode);
    }

    private static void Week(WeekRecord expected, WeekRecord actual)
    {
        Assert.Equal((expected.Week, expected.Action), (actual.Week, actual.Action));
        Assert.Equal(expected.EncounterRolls, actual.EncounterRolls);
        Events(expected.Events, actual.Events);
    }

    private static void Events(IReadOnlyList<EventResolution> expected, IReadOnlyList<EventResolution> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int index = 0; index < expected.Count; index++)
        {
            Assert.Equal((expected[index].EventId, expected[index].Index), (actual[index].EventId, actual[index].Index));
            ResponseResolution? expectedResponse = expected[index].Response;
            ResponseResolution? actualResponse = actual[index].Response;
            if (expectedResponse is null || actualResponse is null)
            {
                Assert.Equal(expectedResponse is null, actualResponse is null);
                continue;
            }

            Assert.Equal(ResponseScalars(expectedResponse), ResponseScalars(actualResponse));
            Assert.Equal(expectedResponse.FlagChanges, actualResponse.FlagChanges);
        }
    }
}
