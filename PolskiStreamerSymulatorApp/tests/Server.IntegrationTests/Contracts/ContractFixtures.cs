using PolskiStreamerSymulatorApp.Contracts.Runs;

namespace PolskiStreamerSymulatorApp.Server.IntegrationTests.Contracts;

/// <summary>
/// Wire-format samples. Their numbers mirror the domain fixtures, but the JSON tests check only the format.
/// </summary>
internal static class ContractFixtures
{
    public static RunStateDto PendingWeek()
    {
        return new RunStateDto(
            SchemaVersion: SaveSchema.CurrentVersion,
            RulesVersion: 1,
            CatalogVersion: 1,
            RunId: Guid.Parse("3f2b8c4e-6a1d-4c2e-9b7a-5d0e8f1a2b3c"),
            StreamerName: "NeonBorsuk",
            Week: 2,
            Status: RunStatusDto.PendingWeek,
            MoneyPln: 1_675,
            Viewers: 33,
            Drama: 50,
            Rng: new RngStateDto("pcg32", Seed: 42, Stream: 54, State: 0x5f0e2b1a9c3d4e7f),
            Ledger:
            [
                new CashFlowEntryDto(1, 0, CashFlowSourceDto.Subscription, 0, CashFlowCategoryDto.Subscriptions, 2),
                new CashFlowEntryDto(2, 0, CashFlowSourceDto.WeeklyActionCost, 0, CashFlowCategoryDto.Expenses, 50),
                new CashFlowEntryDto(2, 0, CashFlowSourceDto.WeeklyActionOutcome, 0, CashFlowCategoryDto.Sponsors, 250),
                new CashFlowEntryDto(2, 0, CashFlowSourceDto.Subscription, 0, CashFlowCategoryDto.Subscriptions, 3),
                new CashFlowEntryDto(2, 1, CashFlowSourceDto.EncounterCost, 0, CashFlowCategoryDto.Expenses, 30),
            ],
            History:
            [
                new WeekRecordDto(
                    1,
                    new ActionResolutionDto("regular_stream", 1_783, "steady", 8, 0),
                    [new EncounterRollDto("meme_misread", 9_001, false)],
                    []),
            ],
            CurrentWeek: new WeekInProgressDto(
                2,
                new ActionResolutionDto("sponsor_pitch", 3_097, "deal", 5, 0),
                [new EncounterRollDto("internet_outage", 120, true)],
                [],
                new PendingEventDto("internet_outage", 1)),
            Flags: [],
            Ending: null);
    }

    public static RunStateDto SpecialEnding()
    {
        return PendingWeek() with
        {
            Week = 1,
            Status = RunStatusDto.SpecialEnding,
            MoneyPln = 1_502,
            Viewers = 23,
            Drama = 40,
            Ledger = [new CashFlowEntryDto(1, 0, CashFlowSourceDto.Subscription, 0, CashFlowCategoryDto.Subscriptions, 2)],
            History =
            [
                new WeekRecordDto(
                    1,
                    new ActionResolutionDto("regular_stream", 1_783, "steady", 8, 0),
                    [new EncounterRollDto("farewell_offer", 40, true)],
                    [
                        new EventResolutionDto(
                            "farewell_offer",
                            1,
                            new ResponseResolutionDto(
                                "accept_offer",
                                9_500,
                                "retire_now",
                                -5,
                                -10,
                                [new FlagChangeDto("farewell_teased", FlagOperationDto.Clear)],
                                "retired")),
                    ]),
            ],
            CurrentWeek = null,
            Flags = [new NarrativeFlagDto("clip_backlash", 1)],
            Ending = new RunEndingDto(RunEndingKindDto.Special, 1, "retired"),
        };
    }
}
