using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using PolskiStreamerSymulatorApp.Contracts.Runs;
using PolskiStreamerSymulatorApp.Contracts.Serialization;

namespace PolskiStreamerSymulatorApp.Server.IntegrationTests.Contracts;

public sealed class ContractJsonTests
{
    private const string PendingWeekJson =
        """{"schemaVersion":1,"rulesVersion":1,"catalogVersion":1,"runId":"3f2b8c4e-6a1d-4c2e-9b7a-5d0e8f1a2b3c","streamerName":"NeonBorsuk","week":2,"status":"pendingWeek","moneyPln":1675,"viewers":33,"drama":50,"rng":{"algorithm":"pcg32","seed":"000000000000002a","stream":"0000000000000036","state":"5f0e2b1a9c3d4e7f"},"ledger":[{"week":1,"step":0,"source":"subscription","ordinal":0,"category":"subscriptions","amountPln":2},{"week":2,"step":0,"source":"weeklyActionCost","ordinal":0,"category":"expenses","amountPln":50},{"week":2,"step":0,"source":"weeklyActionOutcome","ordinal":0,"category":"sponsors","amountPln":250},{"week":2,"step":0,"source":"subscription","ordinal":0,"category":"subscriptions","amountPln":3},{"week":2,"step":1,"source":"encounterCost","ordinal":0,"category":"expenses","amountPln":30}],"history":[{"week":1,"action":{"actionId":"regular_stream","roll":1783,"outcomeId":"steady","viewersDelta":8,"dramaDelta":0},"encounterRolls":[{"eventId":"meme_misread","roll":9001,"passed":false}],"events":[]}],"currentWeek":{"week":2,"action":{"actionId":"sponsor_pitch","roll":3097,"outcomeId":"deal","viewersDelta":5,"dramaDelta":0},"encounterRolls":[{"eventId":"internet_outage","roll":120,"passed":true}],"resolvedEvents":[],"pending":{"eventId":"internet_outage","index":1}},"flags":[],"ending":null}""";

    public static TheoryData<string, string, string> MalformedInputs => new()
    {
        { "missing property", "\"drama\":50,", "" },
        { "missing nullable property", ",\"ending\":null", "" },
        { "null in a non-nullable property", "\"streamerName\":\"NeonBorsuk\"", "\"streamerName\":null" },
        { "null nested object", "\"pending\":{\"eventId\":\"internet_outage\",\"index\":1}", "\"pending\":null" },
        { "null array element", "\"flags\":[]", "\"flags\":[null]" },
        { "null nested array element", "\"events\":[]", "\"events\":[null]" },
        { "unknown property", "{\"schemaVersion\"", "{\"extra\":1,\"schemaVersion\"" },
        { "numeric enum", "\"status\":\"pendingWeek\"", "\"status\":1" },
        { "unknown enum name", "\"status\":\"pendingWeek\"", "\"status\":\"paused\"" },
        { "enum comma list of defined names", "\"status\":\"pendingWeek\"", "\"status\":\"pendingWeek, completed\"" },
        { "enum comma list of an undefined value", "\"status\":\"pendingWeek\"", "\"status\":\"bankrupt, specialEnding\"" },
        { "padded enum name", "\"status\":\"pendingWeek\"", "\"status\":\" pendingWeek\"" },
        { "wrong-case enum name", "\"status\":\"pendingWeek\"", "\"status\":\"PendingWeek\"" },
        { "duplicate property", "\"week\":2,\"status\"", "\"week\":2,\"week\":7,\"status\"" },
        { "short hexadecimal", "\"seed\":\"000000000000002a\"", "\"seed\":\"2a\"" },
        { "uppercase hexadecimal", "\"seed\":\"000000000000002a\"", "\"seed\":\"000000000000002A\"" },
        { "numeric generator value", "\"seed\":\"000000000000002a\"", "\"seed\":42" },
    };

    [Fact]
    public void PendingWeekSerializesToTheDocumentedFormat()
    {
        string json = JsonSerializer.Serialize(ContractFixtures.PendingWeek(), ContractsJsonContext.Default.RunStateDto);

        Assert.Equal(PendingWeekJson, json);
    }

    [Theory]
    [InlineData("pendingWeek")]
    [InlineData("specialEnding")]
    public void StateRoundTripsWithoutChange(string fixture)
    {
        RunStateDto state = fixture == "pendingWeek" ? ContractFixtures.PendingWeek() : ContractFixtures.SpecialEnding();
        string json = JsonSerializer.Serialize(state, ContractsJsonContext.Default.RunStateDto);

        RunStateDto? parsed = JsonSerializer.Deserialize(json, ContractsJsonContext.Default.RunStateDto);

        Assert.NotNull(parsed);
        Assert.Equal(json, JsonSerializer.Serialize(parsed, ContractsJsonContext.Default.RunStateDto));
    }

    [Theory]
    [MemberData(nameof(MalformedInputs))]
    public void MalformedInputIsRejected(string description, string original, string replacement)
    {
        string malformed = PendingWeekJson.Replace(original, replacement, StringComparison.Ordinal);
        Assert.True(malformed != PendingWeekJson, $"The '{description}' case did not change the JSON.");

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(malformed, ContractsJsonContext.Default.RunStateDto));
    }

    [Fact]
    public void GeneratorValuesTravelAsSixteenLowercaseHexadecimalDigits()
    {
        RngStateDto rng = new("pcg32", Seed: 42, Stream: 54, State: ulong.MaxValue);

        string json = JsonSerializer.Serialize(rng, ContractsJsonContext.Default.RngStateDto);

        Assert.Equal("""{"algorithm":"pcg32","seed":"000000000000002a","stream":"0000000000000036","state":"ffffffffffffffff"}""", json);
    }

    [Theory]
    [InlineData(RunStatusDto.Active, "active")]
    [InlineData(RunStatusDto.PendingWeek, "pendingWeek")]
    [InlineData(RunStatusDto.Completed, "completed")]
    [InlineData(RunStatusDto.Bankrupt, "bankrupt")]
    [InlineData(RunStatusDto.SpecialEnding, "specialEnding")]
    public void RunStatusUsesFixedWireNames(RunStatusDto value, string name)
    {
        AssertWireName(value, name, ContractsJsonContext.Default.RunStatusDto);
    }

    [Theory]
    [InlineData(RunEndingKindDto.Completed, "completed")]
    [InlineData(RunEndingKindDto.Bankrupt, "bankrupt")]
    [InlineData(RunEndingKindDto.Special, "special")]
    public void RunEndingKindUsesFixedWireNames(RunEndingKindDto value, string name)
    {
        AssertWireName(value, name, ContractsJsonContext.Default.RunEndingKindDto);
    }

    [Theory]
    [InlineData(FlagOperationDto.Set, "set")]
    [InlineData(FlagOperationDto.Clear, "clear")]
    public void FlagOperationUsesFixedWireNames(FlagOperationDto value, string name)
    {
        AssertWireName(value, name, ContractsJsonContext.Default.FlagOperationDto);
    }

    [Theory]
    [InlineData(CashFlowSourceDto.WeeklyActionCost, "weeklyActionCost")]
    [InlineData(CashFlowSourceDto.WeeklyActionOutcome, "weeklyActionOutcome")]
    [InlineData(CashFlowSourceDto.Subscription, "subscription")]
    [InlineData(CashFlowSourceDto.EncounterCost, "encounterCost")]
    [InlineData(CashFlowSourceDto.ResponseCost, "responseCost")]
    [InlineData(CashFlowSourceDto.ResponseOutcome, "responseOutcome")]
    public void CashFlowSourceUsesFixedWireNames(CashFlowSourceDto value, string name)
    {
        AssertWireName(value, name, ContractsJsonContext.Default.CashFlowSourceDto);
    }

    [Theory]
    [InlineData(CashFlowCategoryDto.Sponsors, "sponsors")]
    [InlineData(CashFlowCategoryDto.Donations, "donations")]
    [InlineData(CashFlowCategoryDto.Subscriptions, "subscriptions")]
    [InlineData(CashFlowCategoryDto.Expenses, "expenses")]
    public void CashFlowCategoryUsesFixedWireNames(CashFlowCategoryDto value, string name)
    {
        AssertWireName(value, name, ContractsJsonContext.Default.CashFlowCategoryDto);
    }

    [Fact]
    public void EveryContractEnumDeclaresTheStrictConverterAndAWireNamePerMember()
    {
        Type[] enums = [.. typeof(RunStateDto).Assembly.GetTypes().Where(static type => type.IsEnum && type.IsPublic)];

        Assert.NotEmpty(enums);
        foreach (Type enumType in enums)
        {
            JsonConverterAttribute? converter = enumType.GetCustomAttribute<JsonConverterAttribute>();
            Assert.NotNull(converter);
            Assert.Equal(typeof(StrictEnumConverter<>).MakeGenericType(enumType), converter.ConverterType);
            Assert.All(
                enumType.GetFields(BindingFlags.Public | BindingFlags.Static),
                static member => Assert.NotNull(member.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()));
        }
    }

    [Fact]
    public void WritingAValueThatIsNotADefinedMemberThrows()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize((RunStatusDto)7, ContractsJsonContext.Default.RunStatusDto));
    }

    private static void AssertWireName<T>(T value, string name, JsonTypeInfo<T> typeInfo)
    {
        string json = $"\"{name}\"";

        Assert.Equal(json, JsonSerializer.Serialize(value, typeInfo));
        Assert.Equal(value, JsonSerializer.Deserialize(json, typeInfo));
    }
}
