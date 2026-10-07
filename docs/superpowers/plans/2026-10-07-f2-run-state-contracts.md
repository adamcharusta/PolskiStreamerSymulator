# F2 Run State and Wire Contracts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Define the domain run state, the PCG32 generator, the cash ledger, the pinned parameters, the weekly-action catalogue shape, terminal reasons, a run-state validator, and the strict JSON wire contracts that every later package uses.

**Architecture:** Domain holds immutable positional records and a `RunStateValidator` that returns either stable error codes or a `ValidatedRunState`, the only form game rules accept. Contracts holds DTO records that mirror the domain records, with one source-generated `ContractsJsonContext` that rejects malformed JSON. No gameplay behavior, endpoint, or mapping is added.

**Tech Stack:** .NET 10, C# records, `System.Text.Json` source generation, xUnit (`xunit.v3` 4.0.1) on Microsoft Testing Platform v2.

**Spec:** `docs/superpowers/specs/2026-10-06-f2-run-state-contracts-design.md`

## Global Constraints

- Shell commands run from `PolskiStreamerSymulatorApp/` unless a step starts with its own `cd`. Run tests only as `dotnet test --project <csproj>` or `dotnet test --solution PolskiStreamerSymulatorApp.sln`; `global.json` there selects Microsoft Testing Platform mode.
- Every build ends with 0 warnings and 0 errors; `Directory.Build.props` treats warnings as errors.
- `Domain` keeps no package or project references. `Contracts` keeps no project references. Test packages already have central versions in `Directory.Packages.props`; add no package.
- Namespaces follow folders: `PolskiStreamerSymulatorApp.Domain.<Folder>`, `PolskiStreamerSymulatorApp.Contracts.<Folder>`, `PolskiStreamerSymulatorApp.Domain.Tests.<Folder>`, `PolskiStreamerSymulatorApp.Server.IntegrationTests.Contracts`.
- C# follows `.editorconfig`: file-scoped namespaces, explicit types instead of `var`, braces, LF line endings.
- Exact values: stable ID pattern `^[a-z][a-z0-9_]{0,63}\z`; PCG32 multiplier `6364136223846793005`, algorithm ID `pcg32`, roll bound 10,000 with rejection threshold 7,296; streamer names 2 to 32 displayed characters and at most 64 UTF-16 code units; run length 1 to 260 weeks; `RulesVersion.Current = 1`; `SaveSchema.CurrentVersion = 1`; locales `pl-PL` (default) and `en`.
- Create and edit files with a file-writing or editing tool, never with shell heredocs or sed. The shell in this environment turns `\\` into `\`, and this code contains `\z`, `́`, `\U0001F600`, `\n`, and `\t`.
- Work stays on branch `feature/f2-run-state-contracts`. Never push. End each commit message with the Co-Authored-By attribution trailer your own environment specifies; the commit commands below show it as `<your Co-Authored-By trailer>`.
- Documentation and code comments are in English. This plan changes no player-facing text.
- `tests/Domain.Tests/` already exists as an empty, untracked folder; the new test project goes into it.

## Review Focus

1. A save edited to hold `null` inside an array must be rejected at the JSON boundary instead of crashing the validator. Owned by Task 5, malformed cases "null array element" and "null nested array element".
2. A name that becomes valid only after normalization, such as one with outer spaces or decomposed letters, must be stored normalized, and the validator must reject a stored name that is not. Owned by Task 2 (`TrimsOuterWhitespace`, `ConvertsDecomposedTextToComposedForm`) and Task 4 (the `invalid_streamer_name` mutation).
3. A bankrupt run whose final event never received a response is legitimate, while an unanswered event anywhere else is not. Owned by Task 4, the bankrupt fixture and the `unanswered_event` mutation.
4. A 64-bit generator value must never travel as a JSON number, because JavaScript tools would round it. Owned by Task 5, `GeneratorValuesTravelAsSixteenLowercaseHexadecimalDigits` and the malformed case "numeric generator value".
5. Ledger amounts large enough to wrap around 64 bits must not reconcile by accident. Owned by Task 4, `LedgerAmountsThatWrapAroundSixtyFourBitsDoNotReconcile`.

## File Structure

| Path under `PolskiStreamerSymulatorApp/` | Responsibility |
| --- | --- |
| `src/Domain/Randomness/RngState.cs`, `Pcg32.cs` | Saved generator position and the PCG32 generator |
| `src/Domain/Identity/StableId.cs`, `StreamerName.cs` | ID format check; streamer name normalization and validation |
| `src/Domain/Catalog/GameParameters.cs`, `GameParametersValidator.cs` | Pinned numeric parameters and their publication rules |
| `src/Domain/Catalog/WeeklyActionDefinition.cs`, `TerminalReasonDefinition.cs` | Catalogue shapes for weekly actions and special endings |
| `src/Domain/Ledger/CashFlowEntry.cs` | Ledger entry, category, and source |
| `src/Domain/Versioning/RulesVersion.cs` | Coded rules version |
| `src/Domain/Runs/RunState.cs`, `WeekRecords.cs` | Run state and its parts |
| `src/Domain/Runs/RunStateValidation.cs`, `RunStateErrorCodes.cs`, `RunStateValidator.cs` | Validation result, error codes, and the validator |
| `src/Contracts/Serialization/*.cs` | JSON context, strict enum and hexadecimal converters, null-element guard |
| `src/Contracts/Runs/*.cs`, `Setup/SetupContracts.cs` | DTOs, requests, responses, and the save schema version |
| `src/Contracts/Problems/ProblemCodes.cs`, `Localization/SupportedLocales.cs` | API problem codes and shipped locales |
| `tests/Domain.Tests/**` | Domain tests |
| `tests/Server.IntegrationTests/Contracts/*.cs` | Wire format tests |

---

### Task 1: Domain test project and the PCG32 generator

**Files:**
- Create: `PolskiStreamerSymulatorApp/tests/Domain.Tests/Domain.Tests.csproj`, `PolskiStreamerSymulatorApp/tests/Domain.Tests/Randomness/Pcg32Tests.cs`
- Create: `PolskiStreamerSymulatorApp/src/Domain/Randomness/RngState.cs`, `PolskiStreamerSymulatorApp/src/Domain/Randomness/Pcg32.cs`
- Modify: `PolskiStreamerSymulatorApp/PolskiStreamerSymulatorApp.sln`

**Interfaces:**
- Consumes: nothing.
- Produces: `public sealed record RngState(string Algorithm, ulong Seed, ulong Stream, ulong State)` and `public sealed class Pcg32` in `PolskiStreamerSymulatorApp.Domain.Randomness`, with `const string AlgorithmId = "pcg32"`, `const uint RollBound = 10_000`, `Pcg32(RngState state)`, `static RngState Seed(ulong seed, ulong stream)`, `uint NextUInt32()`, `uint NextBounded(uint bound)`, `int NextRoll()`, and `RngState ToState()`.

- [ ] **Step 1: Create the test project**

Create `tests/Domain.Tests/Domain.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>PolskiStreamerSymulatorApp.Domain.Tests</RootNamespace>
    <AssemblyName>PolskiStreamerSymulatorApp.Domain.Tests</AssemblyName>
    <OutputType>Exe</OutputType>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="xunit.v3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Domain\Domain.csproj" />
  </ItemGroup>

</Project>
```

Run:

```bash
dotnet sln PolskiStreamerSymulatorApp.sln add tests/Domain.Tests/Domain.Tests.csproj --solution-folder tests
```

Expected: ``Project `tests\Domain.Tests\Domain.Tests.csproj` added to the solution.``

- [ ] **Step 2: Write the failing generator tests**

Create `tests/Domain.Tests/Randomness/Pcg32Tests.cs`:

```csharp
using PolskiStreamerSymulatorApp.Domain.Randomness;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Randomness;

public sealed class Pcg32Tests
{
    [Fact]
    public void SeedFortyTwoStreamFiftyFourMatchesPublishedReferenceOutput()
    {
        // Round 1 of pcg32-demo, published at https://www.pcg-random.org/using-pcg-c-basic.html
        uint[] expected = [0xa15c02b7u, 0x7b47f409u, 0xba1d3330u, 0x83d2f293u, 0xbfa4784bu, 0xcbed606eu];
        Pcg32 generator = new(Pcg32.Seed(42, 54));

        uint[] outputs = [.. Enumerable.Range(0, expected.Length).Select(_ => generator.NextUInt32())];

        Assert.Equal(expected, outputs);
    }

    [Fact]
    public void RollsAreReferenceOutputsModuloTenThousand()
    {
        int[] expected = [1783, 3097, 5824];
        Pcg32 generator = new(Pcg32.Seed(42, 54));

        int[] rolls = [generator.NextRoll(), generator.NextRoll(), generator.NextRoll()];

        Assert.Equal(expected, rolls);
    }

    [Fact]
    public void RollDiscardsOutputsBelowTheRejectionThreshold()
    {
        // From state 0 on stream 0 the first two outputs are 0, which lies below the threshold of 7,296.
        RngState start = new(Pcg32.AlgorithmId, Seed: 0, Stream: 0, State: 0);
        Pcg32 raw = new(start);
        uint first = raw.NextUInt32();
        uint second = raw.NextUInt32();
        uint third = raw.NextUInt32();
        Pcg32 generator = new(start);

        int roll = generator.NextRoll();

        Assert.Equal(0u, first);
        Assert.Equal(0u, second);
        Assert.True(third >= 7_296, $"Third output {third} should pass the threshold.");
        Assert.Equal((int)(third % 10_000), roll);
        Assert.Equal(raw.ToState(), generator.ToState());
    }

    [Fact]
    public void RollsStayWithinZeroToNineThousandNineHundredNinetyNine()
    {
        Pcg32 generator = new(Pcg32.Seed(2026, 10));

        int[] rolls = [.. Enumerable.Range(0, 10_000).Select(_ => generator.NextRoll())];

        Assert.All(rolls, roll => Assert.InRange(roll, 0, 9_999));
    }

    [Fact]
    public void SnapshotResumesTheSameSequence()
    {
        Pcg32 original = new(Pcg32.Seed(7, 3));
        original.NextRoll();
        RngState snapshot = original.ToState();

        int[] fromOriginal = [original.NextRoll(), original.NextRoll(), original.NextRoll()];
        Pcg32 resumed = new(snapshot);
        int[] fromResumed = [resumed.NextRoll(), resumed.NextRoll(), resumed.NextRoll()];

        Assert.Equal(fromOriginal, fromResumed);
    }

    [Fact]
    public void SeedKeepsTheAlgorithmSeedAndStream()
    {
        RngState state = Pcg32.Seed(42, 54);

        Assert.Equal(Pcg32.AlgorithmId, state.Algorithm);
        Assert.Equal(42UL, state.Seed);
        Assert.Equal(54UL, state.Stream);
    }

    [Fact]
    public void UnsupportedAlgorithmIsRejected()
    {
        Assert.Throws<ArgumentException>(() => new Pcg32(new RngState("xorshift", 1, 2, 3)));
    }

    [Fact]
    public void ZeroBoundIsRejected()
    {
        Pcg32 generator = new(Pcg32.Seed(1, 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => generator.NextBounded(0));
    }
}
```

- [ ] **Step 3: Run the tests and see them fail**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj`

Expected: the build fails with error CS0246, because `Pcg32` and `RngState` do not exist yet.

- [ ] **Step 4: Implement the generator**

Create `src/Domain/Randomness/RngState.cs`:

```csharp
namespace PolskiStreamerSymulatorApp.Domain.Randomness;

/// <summary>
/// Saved position of the gameplay random generator. Seed and stream identify the sequence; state is the current position.
/// </summary>
public sealed record RngState(string Algorithm, ulong Seed, ulong Stream, ulong State);
```

Create `src/Domain/Randomness/Pcg32.cs`:

```csharp
namespace PolskiStreamerSymulatorApp.Domain.Randomness;

/// <summary>
/// PCG32 (XSH-RR 64/32) as published in pcg_basic.c by Melissa O'Neill. Every gameplay roll comes from this generator.
/// </summary>
public sealed class Pcg32
{
    public const string AlgorithmId = "pcg32";
    public const uint RollBound = 10_000;

    private const ulong Multiplier = 6364136223846793005UL;

    private readonly ulong _seed;
    private readonly ulong _stream;
    private readonly ulong _increment;
    private ulong _state;

    public Pcg32(RngState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!string.Equals(state.Algorithm, AlgorithmId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unsupported random algorithm '{state.Algorithm}'.", nameof(state));
        }

        _seed = state.Seed;
        _stream = state.Stream;
        _increment = (state.Stream << 1) | 1UL;
        _state = state.State;
    }

    /// <summary>
    /// Seeds a new sequence the way pcg32_srandom_r does.
    /// </summary>
    public static RngState Seed(ulong seed, ulong stream)
    {
        Pcg32 generator = new(new RngState(AlgorithmId, seed, stream, 0UL));
        generator.NextUInt32();
        generator._state = unchecked(generator._state + seed);
        generator.NextUInt32();
        return generator.ToState();
    }

    public uint NextUInt32()
    {
        ulong oldState = _state;
        _state = unchecked((oldState * Multiplier) + _increment);
        uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
        int rotation = (int)(oldState >> 59);
        return (xorShifted >> rotation) | (xorShifted << (-rotation & 31));
    }

    /// <summary>
    /// Returns an unbiased value from 0 to bound minus 1 the way pcg32_boundedrand_r does: outputs below the threshold are discarded.
    /// </summary>
    public uint NextBounded(uint bound)
    {
        ArgumentOutOfRangeException.ThrowIfZero(bound);
        uint threshold = unchecked(0U - bound) % bound;
        while (true)
        {
            uint value = NextUInt32();
            if (value >= threshold)
            {
                return value % bound;
            }
        }
    }

    /// <summary>
    /// Returns a roll from 0 to 9,999 for weighted outcome tables expressed in basis points.
    /// </summary>
    public int NextRoll()
    {
        return (int)NextBounded(RollBound);
    }

    public RngState ToState()
    {
        return new RngState(AlgorithmId, _seed, _stream, _state);
    }
}
```

- [ ] **Step 5: Run the tests and see them pass**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj`

Expected: `Passed!` with `total: 8` and `failed: 0`.

- [ ] **Step 6: Commit**

```bash
cd .. && git add PolskiStreamerSymulatorApp/PolskiStreamerSymulatorApp.sln PolskiStreamerSymulatorApp/tests/Domain.Tests PolskiStreamerSymulatorApp/src/Domain/Randomness && git commit -m "Add Domain test project and the PCG32 generator" -m "<your Co-Authored-By trailer>"
```

---

### Task 2: Stable IDs and streamer names

**Files:**
- Create: `PolskiStreamerSymulatorApp/src/Domain/Identity/StableId.cs`, `PolskiStreamerSymulatorApp/src/Domain/Identity/StreamerName.cs`
- Create: `PolskiStreamerSymulatorApp/tests/Domain.Tests/Identity/StableIdTests.cs`, `PolskiStreamerSymulatorApp/tests/Domain.Tests/Identity/StreamerNameTests.cs`

**Interfaces:**
- Consumes: the Domain test project from Task 1.
- Produces: in `PolskiStreamerSymulatorApp.Domain.Identity`, `public static bool StableId.IsValid(string? value)`; `StreamerName` with constants `MinDisplayedCharacters = 2`, `MaxDisplayedCharacters = 32`, `MaxUtf16Length = 64`, `public static bool TryNormalize(string? input, out string normalized)` (an empty string when invalid), and `public static bool IsNormalizedValid(string? value)`.

- [ ] **Step 1: Write the failing tests**

Create `tests/Domain.Tests/Identity/StableIdTests.cs`:

```csharp
using PolskiStreamerSymulatorApp.Domain.Identity;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Identity;

public sealed class StableIdTests
{
    [Theory]
    [InlineData("regular_stream")]
    [InlineData("clip_backlash")]
    [InlineData("permanent_ban")]
    [InlineData("a")]
    [InlineData("event_2026")]
    public void AcceptsLowercaseSnakeCase(string value)
    {
        Assert.True(StableId.IsValid(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Regular_stream")]
    [InlineData("1st_event")]
    [InlineData("_hidden")]
    [InlineData("regular-stream")]
    [InlineData("regular stream")]
    [InlineData("zażółć")]
    [InlineData("regular_stream\n")]
    public void RejectsOtherText(string value)
    {
        Assert.False(StableId.IsValid(value));
    }

    [Fact]
    public void RejectsNull()
    {
        Assert.False(StableId.IsValid(null));
    }

    [Fact]
    public void AcceptsSixtyFourCharactersAndRejectsSixtyFive()
    {
        Assert.True(StableId.IsValid("a" + new string('b', 63)));
        Assert.False(StableId.IsValid("a" + new string('b', 64)));
    }
}
```

Create `tests/Domain.Tests/Identity/StreamerNameTests.cs`:

```csharp
using System.Text;
using PolskiStreamerSymulatorApp.Domain.Identity;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Identity;

public sealed class StreamerNameTests
{
    [Theory]
    [InlineData("NeonBorsuk")]
    [InlineData("Cichy Kret")]
    [InlineData("Zażółć_gęślą-1")]
    [InlineData("Ab")]
    [InlineData("42")]
    public void AcceptsValidNamesUnchanged(string name)
    {
        bool valid = StreamerName.TryNormalize(name, out string normalized);

        Assert.True(valid);
        Assert.Equal(name, normalized);
        Assert.True(StreamerName.IsNormalizedValid(name));
    }

    [Fact]
    public void TrimsOuterWhitespace()
    {
        bool valid = StreamerName.TryNormalize("  NeonBorsuk \t", out string normalized);

        Assert.True(valid);
        Assert.Equal("NeonBorsuk", normalized);
        Assert.False(StreamerName.IsNormalizedValid("  NeonBorsuk \t"));
    }

    [Fact]
    public void ConvertsDecomposedTextToComposedForm()
    {
        string composed = "Zażółć".Normalize(NormalizationForm.FormC);
        string decomposed = composed.Normalize(NormalizationForm.FormD);

        bool valid = StreamerName.TryNormalize(decomposed, out string normalized);

        Assert.True(valid);
        Assert.NotEqual(decomposed, composed);
        Assert.Equal(composed, normalized);
    }

    [Fact]
    public void AcceptsThirtyTwoDisplayedCharactersAndRejectsThirtyThree()
    {
        Assert.True(StreamerName.TryNormalize(new string('a', 32), out _));
        Assert.False(StreamerName.TryNormalize(new string('a', 33), out _));
    }

    [Fact]
    public void CountsALetterWithACombiningMarkAsOneDisplayedCharacter()
    {
        // q with a combining acute accent has no composed form, so each pair stays two UTF-16 code units.
        string marked = string.Concat(Enumerable.Repeat("q́", 32));

        Assert.True(StreamerName.TryNormalize(marked, out string normalized));
        Assert.Equal(64, normalized.Length);
    }

    [Fact]
    public void RejectsNamesLongerThanSixtyFourUtf16CodeUnits()
    {
        string overlong = "ab" + new string('́', 63);

        Assert.False(StreamerName.TryNormalize(overlong, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    [InlineData("Neon  Borsuk")]
    [InlineData("Neon\tBorsuk")]
    [InlineData("Neon\u0000Borsuk")]
    [InlineData("Neon\U0001F600")]
    [InlineData("Neon!")]
    [InlineData("--")]
    [InlineData("_ -")]
    [InlineData("́ab")]
    public void RejectsInvalidNames(string name)
    {
        bool valid = StreamerName.TryNormalize(name, out string normalized);

        Assert.False(valid);
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void RejectsNull()
    {
        Assert.False(StreamerName.TryNormalize(null, out _));
        Assert.False(StreamerName.IsNormalizedValid(null));
    }
}
```

- [ ] **Step 2: Run the tests and see them fail**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj`

Expected: the build fails with error CS0246, because `StableId` and `StreamerName` do not exist yet.

- [ ] **Step 3: Implement the checks**

Create `src/Domain/Identity/StableId.cs`:

```csharp
using System.Text.RegularExpressions;

namespace PolskiStreamerSymulatorApp.Domain.Identity;

/// <summary>
/// Checks the stable IDs shared by catalogue content, saves, and localized text lookups: lowercase snake case, 1 to 64 characters.
/// </summary>
public static partial class StableId
{
    public static bool IsValid(string? value)
    {
        return value is not null && Pattern().IsMatch(value);
    }

    [GeneratedRegex(@"^[a-z][a-z0-9_]{0,63}\z", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
```

Create `src/Domain/Identity/StreamerName.cs`:

```csharp
using System.Globalization;
using System.Text;

namespace PolskiStreamerSymulatorApp.Domain.Identity;

/// <summary>
/// Normalizes and validates the streamer name a player accepts or types at setup.
/// </summary>
public static class StreamerName
{
    public const int MinDisplayedCharacters = 2;
    public const int MaxDisplayedCharacters = 32;
    public const int MaxUtf16Length = 64;

    /// <summary>
    /// Applies NFC and trims outer whitespace, then checks length and characters. Returns false and an empty string for an invalid name.
    /// </summary>
    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (input is null)
        {
            return false;
        }

        string candidate = input.Normalize(NormalizationForm.FormC).Trim();
        if (candidate.Length > MaxUtf16Length || !HasOnlyAllowedCharacters(candidate))
        {
            return false;
        }

        int displayed = new StringInfo(candidate).LengthInTextElements;
        if (displayed is < MinDisplayedCharacters or > MaxDisplayedCharacters)
        {
            return false;
        }

        normalized = candidate;
        return true;
    }

    /// <summary>
    /// True when the value is valid and already in the form TryNormalize returns, as a saved name must be.
    /// </summary>
    public static bool IsNormalizedValid(string? value)
    {
        return TryNormalize(value, out string normalized) && string.Equals(normalized, value, StringComparison.Ordinal);
    }

    private static bool HasOnlyAllowedCharacters(string candidate)
    {
        bool hasLetterOrDigit = false;
        bool previousIsSpace = false;
        bool previousIsBase = false;
        foreach (Rune rune in candidate.EnumerateRunes())
        {
            UnicodeCategory category = Rune.GetUnicodeCategory(rune);
            if (Rune.IsLetter(rune) || category == UnicodeCategory.DecimalDigitNumber)
            {
                hasLetterOrDigit = true;
                previousIsBase = true;
                previousIsSpace = false;
            }
            else if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark)
            {
                if (!previousIsBase)
                {
                    return false;
                }

                previousIsSpace = false;
            }
            else if (rune.Value is '-' or '_')
            {
                previousIsBase = false;
                previousIsSpace = false;
            }
            else if (rune.Value == ' ' && !previousIsSpace)
            {
                previousIsBase = false;
                previousIsSpace = true;
            }
            else
            {
                return false;
            }
        }

        return hasLetterOrDigit;
    }
}
```

- [ ] **Step 4: Run the tests and see them pass**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj`

Expected: `Passed!` with `total: 45` and `failed: 0`.

- [ ] **Step 5: Commit**

```bash
cd .. && git add PolskiStreamerSymulatorApp/src/Domain/Identity PolskiStreamerSymulatorApp/tests/Domain.Tests/Identity && git commit -m "Add stable ID and streamer name validation" -m "<your Co-Authored-By trailer>"
```

---

### Task 3: Game parameters, ledger entries, and catalogue shapes

**Files:**
- Create: `PolskiStreamerSymulatorApp/src/Domain/Catalog/GameParameters.cs`, `GameParametersValidator.cs`, `WeeklyActionDefinition.cs`, `TerminalReasonDefinition.cs`
- Create: `PolskiStreamerSymulatorApp/src/Domain/Ledger/CashFlowEntry.cs`
- Create: `PolskiStreamerSymulatorApp/tests/Domain.Tests/Catalog/GameParametersValidatorTests.cs`, `PolskiStreamerSymulatorApp/tests/Domain.Tests/Ledger/CashFlowEntryTests.cs`

**Interfaces:**
- Consumes: the Domain test project from Task 1.
- Produces: in `PolskiStreamerSymulatorApp.Domain.Catalog`, `GameParameters` with the twelve positional members in the order shown below, `GameParametersValidator.Validate(GameParameters) : IReadOnlyList<string>` returning the names of broken fields, `WeeklyActionDefinition`, `WeeklyActionOutcome`, `CashFlowAmount`, `TerminalReasonDefinition`, and `TerminalClassification`; in `PolskiStreamerSymulatorApp.Domain.Ledger`, `CashFlowEntry(int Week, int Step, CashFlowSource Source, int Ordinal, CashFlowCategory Category, long AmountPln)` with `long SignedAmountPln`, and the enums `CashFlowCategory` and `CashFlowSource`.

- [ ] **Step 1: Write the failing tests**

Create `tests/Domain.Tests/Catalog/GameParametersValidatorTests.cs`:

```csharp
using PolskiStreamerSymulatorApp.Domain.Catalog;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Catalog;

public sealed class GameParametersValidatorTests
{
    private static readonly GameParameters FirstPublished = new(
        RunLengthWeeks: 52,
        MaxEventsPerWeek: 3,
        StartingMoneyPln: 1_500,
        StartingViewers: 20,
        StartingDrama: 50,
        BankruptcyThresholdPln: -1_000,
        SubscriptionViewersPerPln: 10,
        ScoreAudienceReferenceViewers: 1_000,
        ScoreProfitReferencePln: 5_000,
        ScorePointsPerComponent: 500,
        CalmDramaMax: 33,
        MiddleDramaMax: 66);

    private static readonly Dictionary<string, (GameParameters Parameters, string Field)> Violations = new()
    {
        ["run length 0"] = (FirstPublished with { RunLengthWeeks = 0 }, nameof(GameParameters.RunLengthWeeks)),
        ["run length 261"] = (FirstPublished with { RunLengthWeeks = 261 }, nameof(GameParameters.RunLengthWeeks)),
        ["event cap 0"] = (FirstPublished with { MaxEventsPerWeek = 0 }, nameof(GameParameters.MaxEventsPerWeek)),
        ["starting viewers -1"] = (FirstPublished with { StartingViewers = -1 }, nameof(GameParameters.StartingViewers)),
        ["starting drama -1"] = (FirstPublished with { StartingDrama = -1 }, nameof(GameParameters.StartingDrama)),
        ["starting drama 101"] = (FirstPublished with { StartingDrama = 101 }, nameof(GameParameters.StartingDrama)),
        ["threshold 0"] = (FirstPublished with { BankruptcyThresholdPln = 0 }, nameof(GameParameters.BankruptcyThresholdPln)),
        ["threshold equal to starting money"] =
            (FirstPublished with { StartingMoneyPln = -1_000 }, nameof(GameParameters.BankruptcyThresholdPln)),
        ["subscription divisor 0"] =
            (FirstPublished with { SubscriptionViewersPerPln = 0 }, nameof(GameParameters.SubscriptionViewersPerPln)),
        ["audience reference 0"] =
            (FirstPublished with { ScoreAudienceReferenceViewers = 0 }, nameof(GameParameters.ScoreAudienceReferenceViewers)),
        ["profit reference 0"] = (FirstPublished with { ScoreProfitReferencePln = 0 }, nameof(GameParameters.ScoreProfitReferencePln)),
        ["points per component 0"] =
            (FirstPublished with { ScorePointsPerComponent = 0 }, nameof(GameParameters.ScorePointsPerComponent)),
        ["calm maximum -1"] = (FirstPublished with { CalmDramaMax = -1 }, nameof(GameParameters.CalmDramaMax)),
        ["calm maximum equal to middle"] = (FirstPublished with { CalmDramaMax = 66 }, nameof(GameParameters.CalmDramaMax)),
        ["middle maximum 100"] = (FirstPublished with { MiddleDramaMax = 100 }, nameof(GameParameters.MiddleDramaMax)),
    };

    public static TheoryData<string> ViolationNames => new(Violations.Keys);

    [Fact]
    public void FirstPublishedValuesPass()
    {
        Assert.Empty(GameParametersValidator.Validate(FirstPublished));
    }

    [Fact]
    public void BoundaryValuesInsideTheRulesPass()
    {
        GameParameters[] valid =
        [
            FirstPublished with { RunLengthWeeks = 1 },
            FirstPublished with { RunLengthWeeks = 260 },
            FirstPublished with { StartingViewers = 0 },
            FirstPublished with { StartingDrama = 0 },
            FirstPublished with { StartingDrama = 100 },
            FirstPublished with { CalmDramaMax = 0 },
            FirstPublished with { MiddleDramaMax = 99 },
            FirstPublished with { BankruptcyThresholdPln = -1, StartingMoneyPln = 0 },
        ];

        Assert.All(valid, parameters => Assert.Empty(GameParametersValidator.Validate(parameters)));
    }

    [Theory]
    [MemberData(nameof(ViolationNames))]
    public void ViolationNamesOnlyTheBrokenField(string name)
    {
        (GameParameters parameters, string field) = Violations[name];

        IReadOnlyList<string> errors = GameParametersValidator.Validate(parameters);

        Assert.Equal(new[] { field }, errors);
    }
}
```

Create `tests/Domain.Tests/Ledger/CashFlowEntryTests.cs`:

```csharp
using PolskiStreamerSymulatorApp.Domain.Ledger;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Ledger;

public sealed class CashFlowEntryTests
{
    [Theory]
    [InlineData(CashFlowCategory.Sponsors, 250L)]
    [InlineData(CashFlowCategory.Donations, 20L)]
    [InlineData(CashFlowCategory.Subscriptions, 3L)]
    [InlineData(CashFlowCategory.Expenses, -50L)]
    public void SignedAmountSubtractsOnlyExpenses(CashFlowCategory category, long expected)
    {
        CashFlowEntry entry = new(Week: 1, Step: 0, CashFlowSource.WeeklyActionOutcome, Ordinal: 0, category, Math.Abs(expected));

        Assert.Equal(expected, entry.SignedAmountPln);
    }
}
```

- [ ] **Step 2: Run the tests and see them fail**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj`

Expected: the build fails with error CS0246, because `GameParameters`, `GameParametersValidator`, and `CashFlowEntry` do not exist yet.

- [ ] **Step 3: Implement parameters and ledger entries**

Create `src/Domain/Catalog/GameParameters.cs`:

```csharp
namespace PolskiStreamerSymulatorApp.Domain.Catalog;

/// <summary>
/// Typed numeric parameters of one published catalogue version. A run pins the row of its catalogue version.
/// </summary>
public sealed record GameParameters(
    int RunLengthWeeks,
    int MaxEventsPerWeek,
    long StartingMoneyPln,
    int StartingViewers,
    int StartingDrama,
    long BankruptcyThresholdPln,
    int SubscriptionViewersPerPln,
    int ScoreAudienceReferenceViewers,
    long ScoreProfitReferencePln,
    int ScorePointsPerComponent,
    int CalmDramaMax,
    int MiddleDramaMax);
```

Create `src/Domain/Catalog/GameParametersValidator.cs`:

```csharp
namespace PolskiStreamerSymulatorApp.Domain.Catalog;

/// <summary>
/// Applies the publication rules for a game parameter row. Each error is the name of the field that breaks a rule.
/// </summary>
public static class GameParametersValidator
{
    public const int MinRunLengthWeeks = 1;
    public const int MaxRunLengthWeeks = 260;
    public const int MaxDrama = 100;

    public static IReadOnlyList<string> Validate(GameParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        List<string> errors = [];

        if (parameters.RunLengthWeeks is < MinRunLengthWeeks or > MaxRunLengthWeeks)
        {
            errors.Add(nameof(GameParameters.RunLengthWeeks));
        }

        if (parameters.MaxEventsPerWeek < 1)
        {
            errors.Add(nameof(GameParameters.MaxEventsPerWeek));
        }

        if (parameters.StartingViewers < 0)
        {
            errors.Add(nameof(GameParameters.StartingViewers));
        }

        if (parameters.StartingDrama is < 0 or > MaxDrama)
        {
            errors.Add(nameof(GameParameters.StartingDrama));
        }

        if (parameters.BankruptcyThresholdPln >= 0 || parameters.BankruptcyThresholdPln >= parameters.StartingMoneyPln)
        {
            errors.Add(nameof(GameParameters.BankruptcyThresholdPln));
        }

        if (parameters.SubscriptionViewersPerPln < 1)
        {
            errors.Add(nameof(GameParameters.SubscriptionViewersPerPln));
        }

        if (parameters.ScoreAudienceReferenceViewers < 1)
        {
            errors.Add(nameof(GameParameters.ScoreAudienceReferenceViewers));
        }

        if (parameters.ScoreProfitReferencePln < 1)
        {
            errors.Add(nameof(GameParameters.ScoreProfitReferencePln));
        }

        if (parameters.ScorePointsPerComponent < 1)
        {
            errors.Add(nameof(GameParameters.ScorePointsPerComponent));
        }

        if (parameters.CalmDramaMax < 0 || parameters.CalmDramaMax >= parameters.MiddleDramaMax)
        {
            errors.Add(nameof(GameParameters.CalmDramaMax));
        }

        if (parameters.MiddleDramaMax >= MaxDrama)
        {
            errors.Add(nameof(GameParameters.MiddleDramaMax));
        }

        return errors;
    }
}
```

Create `src/Domain/Ledger/CashFlowEntry.cs`:

```csharp
namespace PolskiStreamerSymulatorApp.Domain.Ledger;

/// <summary>
/// One categorized money change. Week and step point to the week record that names its action, event, option, and outcome.
/// Step 0 is the weekly action step, which also carries the subscription settlement; step k is the week's k-th selected event.
/// </summary>
public sealed record CashFlowEntry(int Week, int Step, CashFlowSource Source, int Ordinal, CashFlowCategory Category, long AmountPln)
{
    public long SignedAmountPln => Category == CashFlowCategory.Expenses ? -AmountPln : AmountPln;
}

public enum CashFlowCategory
{
    Sponsors,
    Donations,
    Subscriptions,
    Expenses,
}

public enum CashFlowSource
{
    WeeklyActionCost,
    WeeklyActionOutcome,
    Subscription,
    EncounterCost,
    ResponseCost,
    ResponseOutcome,
}
```

- [ ] **Step 4: Run the tests and see them pass**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj`

Expected: `Passed!` with `total: 66` and `failed: 0`.

- [ ] **Step 5: Add the catalogue shapes**

These records only fix shapes; S1 and S2 add their rules and tests.

Create `src/Domain/Catalog/WeeklyActionDefinition.cs`:

```csharp
using PolskiStreamerSymulatorApp.Domain.Ledger;

namespace PolskiStreamerSymulatorApp.Domain.Catalog;

/// <summary>
/// One weekly action of a published catalogue: a guaranteed cost paid before the roll and its weighted outcomes.
/// </summary>
public sealed record WeeklyActionDefinition(string ActionId, long GuaranteedCostPln, IReadOnlyList<WeeklyActionOutcome> Outcomes);

/// <summary>
/// One weighted result of a weekly action. Chances are basis points, where 10,000 is 100%.
/// </summary>
public sealed record WeeklyActionOutcome(
    string OutcomeId,
    int ChanceBps,
    int ViewersDelta,
    int DramaDelta,
    IReadOnlyList<CashFlowAmount> CashFlows);

/// <summary>
/// A sponsor or donation income, or an expense, that an outcome adds to the ledger.
/// </summary>
public sealed record CashFlowAmount(CashFlowCategory Category, long AmountPln);
```

Create `src/Domain/Catalog/TerminalReasonDefinition.cs`:

```csharp
namespace PolskiStreamerSymulatorApp.Domain.Catalog;

/// <summary>
/// A special early ending a rolled event outcome may carry, with its pinned classification.
/// </summary>
public sealed record TerminalReasonDefinition(string ReasonCode, TerminalClassification Classification);

public enum TerminalClassification
{
    Success,
    Neutral,
    Defeat,
}
```

Run: `dotnet build PolskiStreamerSymulatorApp.sln`

Expected: `0 Warning(s)` and `0 Error(s)`.

- [ ] **Step 6: Commit**

```bash
cd .. && git add PolskiStreamerSymulatorApp/src/Domain/Catalog PolskiStreamerSymulatorApp/src/Domain/Ledger PolskiStreamerSymulatorApp/tests/Domain.Tests/Catalog PolskiStreamerSymulatorApp/tests/Domain.Tests/Ledger && git commit -m "Add game parameters, ledger entries, and catalogue shapes" -m "<your Co-Authored-By trailer>"
```

---

### Task 4: Run state and its validator

**Files:**
- Create: `PolskiStreamerSymulatorApp/src/Domain/Versioning/RulesVersion.cs`
- Create: `PolskiStreamerSymulatorApp/src/Domain/Runs/RunState.cs`, `WeekRecords.cs`, `RunStateValidation.cs`, `RunStateErrorCodes.cs`, `RunStateValidator.cs`
- Create: `PolskiStreamerSymulatorApp/tests/Domain.Tests/Runs/RunStateFixtures.cs`, `PolskiStreamerSymulatorApp/tests/Domain.Tests/Runs/RunStateValidatorTests.cs`

**Interfaces:**
- Consumes: `Pcg32` and `RngState` (Task 1), `StableId` and `StreamerName` (Task 2), `GameParameters`, `GameParametersValidator`, `CashFlowEntry`, `CashFlowCategory`, and `CashFlowSource` (Task 3).
- Produces: `RulesVersion.Current = 1` in `PolskiStreamerSymulatorApp.Domain.Versioning`; in `PolskiStreamerSymulatorApp.Domain.Runs`, the records `RunState`, `WeekRecord`, `WeekInProgress`, `ActionResolution`, `EncounterRoll`, `PendingEvent`, `EventResolution`, `ResponseResolution`, `FlagChange`, `NarrativeFlag`, `RunEnding`, `RunStateError`, the enums `RunStatus`, `FlagOperation`, `RunEndingKind`, the classes `RunStateValidation` (`Errors`, `Value`, `IsValid`) and `ValidatedRunState` (`State`, `Parameters`, internal constructor), `RunStateErrorCodes` with `All`, and `public static RunStateValidation RunStateValidator.Validate(RunState state, GameParameters parameters)`.

- [ ] **Step 1: Create the run-state model**

Create `src/Domain/Versioning/RulesVersion.cs`:

```csharp
namespace PolskiStreamerSymulatorApp.Domain.Versioning;

/// <summary>
/// Version of the coded game rules, including the random generator. A saved run must carry the current value.
/// </summary>
public static class RulesVersion
{
    public const int Current = 1;
}
```

Create `src/Domain/Runs/RunState.cs`:

```csharp
using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Randomness;

namespace PolskiStreamerSymulatorApp.Domain.Runs;

/// <summary>
/// One career as saved in the browser. Untrusted until RunStateValidator turns it into a ValidatedRunState.
/// </summary>
public sealed record RunState(
    int RulesVersion,
    int CatalogVersion,
    Guid RunId,
    string StreamerName,
    int Week,
    RunStatus Status,
    long MoneyPln,
    int Viewers,
    int Drama,
    RngState Rng,
    IReadOnlyList<CashFlowEntry> Ledger,
    IReadOnlyList<WeekRecord> History,
    WeekInProgress? CurrentWeek,
    IReadOnlyList<NarrativeFlag> Flags,
    RunEnding? Ending);

public enum RunStatus
{
    Active,
    PendingWeek,
    Completed,
    Bankrupt,
    SpecialEnding,
}

/// <summary>
/// A browser-local story flag and the week a rolled result set it.
/// </summary>
public sealed record NarrativeFlag(string FlagId, int SetWeek);

/// <summary>
/// How and when a run ended. Only a special ending carries a reason code.
/// </summary>
public sealed record RunEnding(RunEndingKind Kind, int Week, string? ReasonCode);

public enum RunEndingKind
{
    Completed,
    Bankrupt,
    Special,
}
```

Create `src/Domain/Runs/WeekRecords.cs`:

```csharp
namespace PolskiStreamerSymulatorApp.Domain.Runs;

/// <summary>
/// One finished week: its weekly action, every encounter roll, and every selected event in selection order.
/// </summary>
public sealed record WeekRecord(
    int Week,
    ActionResolution Action,
    IReadOnlyList<EncounterRoll> EncounterRolls,
    IReadOnlyList<EventResolution> Events);

/// <summary>
/// The current week while a selected event waits for the player's response.
/// </summary>
public sealed record WeekInProgress(
    int Week,
    ActionResolution Action,
    IReadOnlyList<EncounterRoll> EncounterRolls,
    IReadOnlyList<EventResolution> ResolvedEvents,
    PendingEvent Pending);

/// <summary>
/// The rolled weekly action. Deltas are the changes actually applied after viewers and drama were clamped.
/// </summary>
public sealed record ActionResolution(string ActionId, int Roll, string OutcomeId, int ViewersDelta, int DramaDelta);

/// <summary>
/// The single encounter roll an event received in a week, and whether it passed its occurrence chance.
/// </summary>
public sealed record EncounterRoll(string EventId, int Roll, bool Passed);

/// <summary>
/// The selected event waiting for a response; its index is its position among the week's selected events.
/// </summary>
public sealed record PendingEvent(string EventId, int Index);

/// <summary>
/// A selected event. The response is null only when the event's encounter cost bankrupted the run.
/// </summary>
public sealed record EventResolution(string EventId, int Index, ResponseResolution? Response);

/// <summary>
/// The chosen response and its rolled outcome. A terminal reason code marks a special early ending.
/// </summary>
public sealed record ResponseResolution(
    string OptionId,
    int Roll,
    string OutcomeId,
    int ViewersDelta,
    int DramaDelta,
    IReadOnlyList<FlagChange> FlagChanges,
    string? TerminalReasonCode);

public sealed record FlagChange(string FlagId, FlagOperation Operation);

public enum FlagOperation
{
    Set,
    Clear,
}
```

Create `src/Domain/Runs/RunStateValidation.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;
using PolskiStreamerSymulatorApp.Domain.Catalog;

namespace PolskiStreamerSymulatorApp.Domain.Runs;

/// <summary>
/// Outcome of validating a run state: either errors or a state the game rules may use.
/// </summary>
public sealed class RunStateValidation
{
    private RunStateValidation(IReadOnlyList<RunStateError> errors, ValidatedRunState? value)
    {
        Errors = errors;
        Value = value;
    }

    public IReadOnlyList<RunStateError> Errors { get; }

    public ValidatedRunState? Value { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsValid => Value is not null;

    internal static RunStateValidation Success(ValidatedRunState value)
    {
        return new RunStateValidation([], value);
    }

    internal static RunStateValidation Failure(IReadOnlyList<RunStateError> errors)
    {
        return new RunStateValidation(errors, null);
    }
}

/// <summary>
/// A run state that passed validation against the parameters of its pinned catalogue version. Only RunStateValidator creates it.
/// </summary>
public sealed class ValidatedRunState
{
    internal ValidatedRunState(RunState state, GameParameters parameters)
    {
        State = state;
        Parameters = parameters;
    }

    public RunState State { get; }

    public GameParameters Parameters { get; }
}

/// <summary>
/// One validation failure: a stable code from RunStateErrorCodes and the path of the offending value.
/// </summary>
public sealed record RunStateError(string Code, string Path);
```

Create `src/Domain/Runs/RunStateErrorCodes.cs`:

```csharp
namespace PolskiStreamerSymulatorApp.Domain.Runs;

/// <summary>
/// Stable codes for run-state validation failures. The F2 design spec states the rule behind each code.
/// </summary>
public static class RunStateErrorCodes
{
    public const string InvalidGameParameters = "invalid_game_parameters";
    public const string UnsupportedRulesVersion = "unsupported_rules_version";
    public const string InvalidCatalogVersion = "invalid_catalog_version";
    public const string InvalidRunId = "invalid_run_id";
    public const string InvalidStreamerName = "invalid_streamer_name";
    public const string UnsupportedRngAlgorithm = "unsupported_rng_algorithm";
    public const string WeekOutOfRange = "week_out_of_range";
    public const string ViewersNegative = "viewers_negative";
    public const string DramaOutOfRange = "drama_out_of_range";
    public const string InvalidStableId = "invalid_stable_id";
    public const string StatusInconsistent = "status_inconsistent";
    public const string EndingInconsistent = "ending_inconsistent";
    public const string MoneyAtOrBelowBankruptcyThreshold = "money_at_or_below_bankruptcy_threshold";
    public const string BankruptMoneyAboveThreshold = "bankrupt_money_above_threshold";
    public const string HistoryInconsistent = "history_inconsistent";
    public const string EventCapExceeded = "event_cap_exceeded";
    public const string EventIndexInconsistent = "event_index_inconsistent";
    public const string EventSelectedTwice = "event_selected_twice";
    public const string EncounterRolledTwice = "encounter_rolled_twice";
    public const string SelectedEventWithoutPassingRoll = "selected_event_without_passing_roll";
    public const string UnansweredEvent = "unanswered_event";
    public const string TerminalReasonNotApplied = "terminal_reason_not_applied";
    public const string RollOutOfRange = "roll_out_of_range";
    public const string LedgerWeekOutOfRange = "ledger_week_out_of_range";
    public const string LedgerStepInvalid = "ledger_step_invalid";
    public const string LedgerCategoryMismatch = "ledger_category_mismatch";
    public const string LedgerAmountInvalid = "ledger_amount_invalid";
    public const string LedgerDuplicateEntry = "ledger_duplicate_entry";
    public const string SubscriptionEntryCount = "subscription_entry_count";
    public const string MoneyNotReconciled = "money_not_reconciled";
    public const string ViewersNotReconciled = "viewers_not_reconciled";
    public const string DramaNotReconciled = "drama_not_reconciled";
    public const string FlagInvalid = "flag_invalid";

    public static IReadOnlyList<string> All { get; } =
    [
        InvalidGameParameters,
        UnsupportedRulesVersion,
        InvalidCatalogVersion,
        InvalidRunId,
        InvalidStreamerName,
        UnsupportedRngAlgorithm,
        WeekOutOfRange,
        ViewersNegative,
        DramaOutOfRange,
        InvalidStableId,
        StatusInconsistent,
        EndingInconsistent,
        MoneyAtOrBelowBankruptcyThreshold,
        BankruptMoneyAboveThreshold,
        HistoryInconsistent,
        EventCapExceeded,
        EventIndexInconsistent,
        EventSelectedTwice,
        EncounterRolledTwice,
        SelectedEventWithoutPassingRoll,
        UnansweredEvent,
        TerminalReasonNotApplied,
        RollOutOfRange,
        LedgerWeekOutOfRange,
        LedgerStepInvalid,
        LedgerCategoryMismatch,
        LedgerAmountInvalid,
        LedgerDuplicateEntry,
        SubscriptionEntryCount,
        MoneyNotReconciled,
        ViewersNotReconciled,
        DramaNotReconciled,
        FlagInvalid,
    ];
}
```

- [ ] **Step 2: Write the validator tests and fixtures**

Create `tests/Domain.Tests/Runs/RunStateFixtures.cs`:

```csharp
using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Randomness;
using PolskiStreamerSymulatorApp.Domain.Runs;
using PolskiStreamerSymulatorApp.Domain.Versioning;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Runs;

/// <summary>
/// Internally consistent run states, one per status, built from sample catalogue IDs.
/// </summary>
internal static class RunStateFixtures
{
    public static readonly Guid RunId = Guid.Parse("3f2b8c4e-6a1d-4c2e-9b7a-5d0e8f1a2b3c");

    public static GameParameters Parameters { get; } = new(
        RunLengthWeeks: 52,
        MaxEventsPerWeek: 3,
        StartingMoneyPln: 1_500,
        StartingViewers: 20,
        StartingDrama: 50,
        BankruptcyThresholdPln: -1_000,
        SubscriptionViewersPerPln: 10,
        ScoreAudienceReferenceViewers: 1_000,
        ScoreProfitReferencePln: 5_000,
        ScorePointsPerComponent: 500,
        CalmDramaMax: 33,
        MiddleDramaMax: 66);

    /// <summary>A two-week configuration, so a completed run stays short.</summary>
    public static GameParameters TwoWeekParameters { get; } = Parameters with { RunLengthWeeks = 2 };

    /// <summary>A configuration that starts in debt, so one encounter cost can bankrupt the run.</summary>
    public static GameParameters InDebtParameters { get; } = Parameters with { StartingMoneyPln = -950 };

    public static CashFlowEntry Subscription(int week, long amountPln)
    {
        return new CashFlowEntry(week, Step: 0, CashFlowSource.Subscription, Ordinal: 0, CashFlowCategory.Subscriptions, amountPln);
    }

    /// <summary>A regular stream with the steady outcome, one failed encounter roll, and no event.</summary>
    public static WeekRecord QuietFirstWeek()
    {
        return new WeekRecord(
            Week: 1,
            Action: new ActionResolution("regular_stream", Roll: 1_783, OutcomeId: "steady", ViewersDelta: 8, DramaDelta: 0),
            EncounterRolls: [new EncounterRoll("meme_misread", Roll: 9_001, Passed: false)],
            Events: []);
    }

    /// <summary>Week 2 is next: 1,502 PLN, 28 viewers, drama 50.</summary>
    public static RunState Active()
    {
        return new RunState(
            RulesVersion: RulesVersion.Current,
            CatalogVersion: 1,
            RunId: RunId,
            StreamerName: "NeonBorsuk",
            Week: 2,
            Status: RunStatus.Active,
            MoneyPln: 1_502,
            Viewers: 28,
            Drama: 50,
            Rng: Pcg32.Seed(42, 54),
            Ledger: [Subscription(1, 2)],
            History: [QuietFirstWeek()],
            CurrentWeek: null,
            Flags: [],
            Ending: null);
    }

    /// <summary>Week 2 waits for a response to an internet outage whose 30 PLN encounter cost is already paid.</summary>
    public static RunState PendingWeek()
    {
        return Active() with
        {
            Status = RunStatus.PendingWeek,
            MoneyPln = 1_675,
            Viewers = 33,
            Ledger =
            [
                Subscription(1, 2),
                new CashFlowEntry(2, 0, CashFlowSource.WeeklyActionCost, 0, CashFlowCategory.Expenses, 50),
                new CashFlowEntry(2, 0, CashFlowSource.WeeklyActionOutcome, 0, CashFlowCategory.Sponsors, 250),
                Subscription(2, 3),
                new CashFlowEntry(2, 1, CashFlowSource.EncounterCost, 0, CashFlowCategory.Expenses, 30),
            ],
            CurrentWeek = new WeekInProgress(
                Week: 2,
                Action: new ActionResolution("sponsor_pitch", Roll: 3_097, OutcomeId: "deal", ViewersDelta: 5, DramaDelta: 0),
                EncounterRolls: [new EncounterRoll("internet_outage", Roll: 120, Passed: true)],
                ResolvedEvents: [],
                Pending: new PendingEvent("internet_outage", Index: 1)),
        };
    }

    /// <summary>A two-week run that finished after two quiet regular streams. Validate with TwoWeekParameters.</summary>
    public static RunState Completed()
    {
        WeekRecord secondWeek = new(
            Week: 2,
            Action: new ActionResolution("regular_stream", Roll: 5_824, OutcomeId: "steady", ViewersDelta: 8, DramaDelta: 0),
            EncounterRolls: [],
            Events: []);
        return Active() with
        {
            Status = RunStatus.Completed,
            MoneyPln = 1_505,
            Viewers = 36,
            Ledger = [Subscription(1, 2), Subscription(2, 3)],
            History = [QuietFirstWeek(), secondWeek],
            Ending = new RunEnding(RunEndingKind.Completed, Week: 2, ReasonCode: null),
        };
    }

    /// <summary>Week 1 ended at a 60 PLN encounter cost that took money from -948 to -1,008. Validate with InDebtParameters.</summary>
    public static RunState Bankrupt()
    {
        WeekRecord week = QuietFirstWeek() with
        {
            EncounterRolls = [new EncounterRoll("rented_studio", Roll: 55, Passed: true)],
            Events = [new EventResolution("rented_studio", Index: 1, Response: null)],
        };
        return Active() with
        {
            Week = 1,
            Status = RunStatus.Bankrupt,
            MoneyPln = -1_008,
            Ledger = [Subscription(1, 2), new CashFlowEntry(1, 1, CashFlowSource.EncounterCost, 0, CashFlowCategory.Expenses, 60)],
            History = [week],
            Ending = new RunEnding(RunEndingKind.Bankrupt, Week: 1, ReasonCode: null),
        };
    }

    /// <summary>Week 1 ended in retirement after the streamer accepted a farewell offer.</summary>
    public static RunState SpecialEnding()
    {
        WeekRecord week = QuietFirstWeek() with
        {
            EncounterRolls = [new EncounterRoll("farewell_offer", Roll: 40, Passed: true)],
            Events =
            [
                new EventResolution(
                    "farewell_offer",
                    Index: 1,
                    Response: new ResponseResolution(
                        OptionId: "accept_offer",
                        Roll: 9_500,
                        OutcomeId: "retire_now",
                        ViewersDelta: -5,
                        DramaDelta: -10,
                        FlagChanges: [],
                        TerminalReasonCode: "retired")),
            ],
        };
        return Active() with
        {
            Week = 1,
            Status = RunStatus.SpecialEnding,
            Viewers = 23,
            Drama = 40,
            History = [week],
            Ending = new RunEnding(RunEndingKind.Special, Week: 1, ReasonCode: "retired"),
        };
    }
}
```

Create `tests/Domain.Tests/Runs/RunStateValidatorTests.cs`:

```csharp
using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Randomness;
using PolskiStreamerSymulatorApp.Domain.Runs;
using PolskiStreamerSymulatorApp.Domain.Versioning;

namespace PolskiStreamerSymulatorApp.Domain.Tests.Runs;

public sealed class RunStateValidatorTests
{
    private static readonly Dictionary<string, Func<(RunState State, GameParameters Parameters)>> Mutations = new()
    {
        [RunStateErrorCodes.InvalidGameParameters] = static () =>
            (RunStateFixtures.Active(), RunStateFixtures.Parameters with { RunLengthWeeks = 0 }),
        [RunStateErrorCodes.UnsupportedRulesVersion] = static () =>
            (RunStateFixtures.Active() with { RulesVersion = RulesVersion.Current + 1 }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.InvalidCatalogVersion] = static () =>
            (RunStateFixtures.Active() with { CatalogVersion = 0 }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.InvalidRunId] = static () =>
            (RunStateFixtures.Active() with { RunId = Guid.Empty }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.InvalidStreamerName] = static () =>
            (RunStateFixtures.Active() with { StreamerName = " NeonBorsuk" }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.UnsupportedRngAlgorithm] = static () =>
            (RunStateFixtures.Active() with { Rng = Pcg32.Seed(42, 54) with { Algorithm = "xorshift" } }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.WeekOutOfRange] = static () =>
            (RunStateFixtures.Active(), RunStateFixtures.Parameters with { RunLengthWeeks = 1 }),
        [RunStateErrorCodes.ViewersNegative] = static () =>
            (RunStateFixtures.Active() with { Viewers = -1 }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.DramaOutOfRange] = static () =>
            (RunStateFixtures.Active() with { Drama = 101 }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.InvalidStableId] = static () =>
            WithFirstWeek(static week => week with { Action = week.Action with { ActionId = "Regular-Stream" } }),
        [RunStateErrorCodes.StatusInconsistent] = static () =>
            (RunStateFixtures.Active() with { Status = RunStatus.PendingWeek }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.EndingInconsistent] = static () =>
            (RunStateFixtures.Completed() with { Ending = new RunEnding(RunEndingKind.Completed, Week: 2, ReasonCode: "retired") },
                RunStateFixtures.TwoWeekParameters),
        [RunStateErrorCodes.MoneyAtOrBelowBankruptcyThreshold] = static () =>
            (RunStateFixtures.Active() with { MoneyPln = -1_000 }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.BankruptMoneyAboveThreshold] = static () =>
            (RunStateFixtures.Bankrupt() with { MoneyPln = -999 }, RunStateFixtures.InDebtParameters),
        [RunStateErrorCodes.HistoryInconsistent] = static () =>
            (RunStateFixtures.Active() with { History = [] }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.EventCapExceeded] = static () =>
            (TwoEventPendingWeek(), RunStateFixtures.Parameters with { MaxEventsPerWeek = 1 }),
        [RunStateErrorCodes.EventIndexInconsistent] = static () =>
            WithCurrentWeek(static week => week with { Pending = week.Pending with { Index = 2 } }),
        [RunStateErrorCodes.EventSelectedTwice] = static () =>
            WithCurrentWeek(static week => week with
            {
                ResolvedEvents = [AnsweredEvent("internet_outage", 1)],
                Pending = week.Pending with { Index = 2 },
            }),
        [RunStateErrorCodes.EncounterRolledTwice] = static () =>
            WithCurrentWeek(static week => week with
            {
                EncounterRolls = [.. week.EncounterRolls, new EncounterRoll("internet_outage", 7_000, false)],
            }),
        [RunStateErrorCodes.SelectedEventWithoutPassingRoll] = static () =>
            WithCurrentWeek(static week => week with { EncounterRolls = [new EncounterRoll("internet_outage", 120, false)] }),
        [RunStateErrorCodes.UnansweredEvent] = static () =>
            WithFirstWeek(static week => week with
            {
                EncounterRolls = [new EncounterRoll("meme_misread", 300, true)],
                Events = [new EventResolution("meme_misread", 1, Response: null)],
            }),
        [RunStateErrorCodes.TerminalReasonNotApplied] = static () =>
            WithFirstWeek(static week => week with
            {
                EncounterRolls = [new EncounterRoll("meme_misread", 300, true)],
                Events = [new EventResolution("meme_misread", 1, Answer() with { TerminalReasonCode = "retired" })],
            }),
        [RunStateErrorCodes.RollOutOfRange] = static () =>
            WithFirstWeek(static week => week with { Action = week.Action with { Roll = 10_000 } }),
        [RunStateErrorCodes.LedgerWeekOutOfRange] = static () =>
            (RunStateFixtures.Active() with
            {
                Ledger =
                [
                    RunStateFixtures.Subscription(1, 2),
                    new CashFlowEntry(5, 0, CashFlowSource.WeeklyActionOutcome, 0, CashFlowCategory.Donations, 10),
                ],
            }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.LedgerStepInvalid] = static () =>
            (RunStateFixtures.Active() with
            {
                Ledger = [new CashFlowEntry(1, 1, CashFlowSource.Subscription, 0, CashFlowCategory.Subscriptions, 2)],
            }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.LedgerCategoryMismatch] = static () =>
            (RunStateFixtures.Active() with
            {
                Ledger = [new CashFlowEntry(1, 0, CashFlowSource.Subscription, 0, CashFlowCategory.Sponsors, 2)],
            }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.LedgerAmountInvalid] = static () =>
            WithExtraPendingWeekEntry(new CashFlowEntry(2, 0, CashFlowSource.WeeklyActionOutcome, 1, CashFlowCategory.Donations, 0)),
        [RunStateErrorCodes.LedgerDuplicateEntry] = static () =>
            WithExtraPendingWeekEntry(new CashFlowEntry(2, 0, CashFlowSource.WeeklyActionOutcome, 0, CashFlowCategory.Donations, 20)),
        [RunStateErrorCodes.SubscriptionEntryCount] = static () =>
            (RunStateFixtures.Active() with { Ledger = [] }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.MoneyNotReconciled] = static () =>
            (RunStateFixtures.Active() with { MoneyPln = 1_503 }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.ViewersNotReconciled] = static () =>
            (RunStateFixtures.Active() with { Viewers = 29 }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.DramaNotReconciled] = static () =>
            (RunStateFixtures.Active() with { Drama = 49 }, RunStateFixtures.Parameters),
        [RunStateErrorCodes.FlagInvalid] = static () =>
            (RunStateFixtures.Active() with { Flags = [new NarrativeFlag("clip_backlash", SetWeek: 3)] }, RunStateFixtures.Parameters),
    };

    public static TheoryData<string> ValidFixtureNames => ["active", "pendingWeek", "completed", "bankrupt", "specialEnding"];

    public static TheoryData<string> ErrorCodes => new(Mutations.Keys);

    [Theory]
    [MemberData(nameof(ValidFixtureNames))]
    public void ValidFixturePassesAndYieldsAValidatedState(string name)
    {
        (RunState state, GameParameters parameters) = ValidFixture(name);

        RunStateValidation result = RunStateValidator.Validate(state, parameters);

        Assert.Empty(result.Errors);
        Assert.True(result.IsValid);
        Assert.NotNull(result.Value);
        Assert.Same(state, result.Value.State);
        Assert.Same(parameters, result.Value.Parameters);
    }

    [Theory]
    [MemberData(nameof(ErrorCodes))]
    public void SmallestMutationProducesItsErrorCode(string code)
    {
        (RunState state, GameParameters parameters) = Mutations[code]();

        RunStateValidation result = RunStateValidator.Validate(state, parameters);

        Assert.False(result.IsValid);
        Assert.Null(result.Value);
        Assert.Contains(code, result.Errors.Select(error => error.Code));
    }

    [Fact]
    public void EveryErrorCodeHasAMutation()
    {
        Assert.Equal(RunStateErrorCodes.All.Order(StringComparer.Ordinal), Mutations.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ReportsEveryErrorItFinds()
    {
        RunState state = RunStateFixtures.Active() with { Viewers = -1, Drama = 101 };

        RunStateValidation result = RunStateValidator.Validate(state, RunStateFixtures.Parameters);

        string[] codes = [.. result.Errors.Select(error => error.Code)];
        Assert.Contains(RunStateErrorCodes.ViewersNegative, codes);
        Assert.Contains(RunStateErrorCodes.DramaOutOfRange, codes);
    }

    [Fact]
    public void InvalidParametersStopValidationWithOneError()
    {
        RunState state = RunStateFixtures.Active() with { Viewers = -1 };

        RunStateValidation result = RunStateValidator.Validate(state, RunStateFixtures.Parameters with { RunLengthWeeks = 0 });

        RunStateError error = Assert.Single(result.Errors);
        Assert.Equal(new RunStateError(RunStateErrorCodes.InvalidGameParameters, "parameters"), error);
    }

    [Fact]
    public void ErrorsCarryThePathOfTheOffendingValue()
    {
        (RunState state, GameParameters parameters) = WithFirstWeek(static week => week with { Action = week.Action with { Roll = 10_000 } });

        RunStateValidation result = RunStateValidator.Validate(state, parameters);

        RunStateError error = Assert.Single(result.Errors);
        Assert.Equal(new RunStateError(RunStateErrorCodes.RollOutOfRange, "history[0].action.roll"), error);
    }

    [Fact]
    public void LedgerAmountsThatWrapAroundSixtyFourBitsDoNotReconcile()
    {
        // In 64-bit arithmetic these three amounts sum to exactly zero, so a wrapping sum would accept the money total.
        RunState state = RunStateFixtures.Active() with
        {
            Ledger =
            [
                RunStateFixtures.Subscription(1, 2),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 0, CashFlowCategory.Sponsors, long.MaxValue),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 1, CashFlowCategory.Donations, long.MaxValue),
                new CashFlowEntry(1, 0, CashFlowSource.WeeklyActionOutcome, 2, CashFlowCategory.Donations, 2),
            ],
        };

        RunStateValidation result = RunStateValidator.Validate(state, RunStateFixtures.Parameters);

        Assert.Contains(RunStateErrorCodes.MoneyNotReconciled, result.Errors.Select(error => error.Code));
    }

    private static (RunState State, GameParameters Parameters) ValidFixture(string name)
    {
        return name switch
        {
            "active" => (RunStateFixtures.Active(), RunStateFixtures.Parameters),
            "pendingWeek" => (RunStateFixtures.PendingWeek(), RunStateFixtures.Parameters),
            "completed" => (RunStateFixtures.Completed(), RunStateFixtures.TwoWeekParameters),
            "bankrupt" => (RunStateFixtures.Bankrupt(), RunStateFixtures.InDebtParameters),
            "specialEnding" => (RunStateFixtures.SpecialEnding(), RunStateFixtures.Parameters),
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown fixture."),
        };
    }

    private static (RunState State, GameParameters Parameters) WithFirstWeek(Func<WeekRecord, WeekRecord> change)
    {
        RunState state = RunStateFixtures.Active();
        return (state with { History = [change(state.History[0])] }, RunStateFixtures.Parameters);
    }

    private static (RunState State, GameParameters Parameters) WithCurrentWeek(Func<WeekInProgress, WeekInProgress> change)
    {
        RunState state = RunStateFixtures.PendingWeek();
        return (state with { CurrentWeek = change(state.CurrentWeek!) }, RunStateFixtures.Parameters);
    }

    private static (RunState State, GameParameters Parameters) WithExtraPendingWeekEntry(CashFlowEntry entry)
    {
        RunState state = RunStateFixtures.PendingWeek();
        return (state with { Ledger = [.. state.Ledger, entry] }, RunStateFixtures.Parameters);
    }

    private static RunState TwoEventPendingWeek()
    {
        RunState state = RunStateFixtures.PendingWeek();
        WeekInProgress week = state.CurrentWeek!;
        return state with
        {
            CurrentWeek = week with
            {
                EncounterRolls = [.. week.EncounterRolls, new EncounterRoll("mysterious_donation", 200, true)],
                ResolvedEvents = [AnsweredEvent("mysterious_donation", 1)],
                Pending = week.Pending with { Index = 2 },
            },
        };
    }

    private static ResponseResolution Answer()
    {
        return new ResponseResolution(
            OptionId: "polite_refusal",
            Roll: 100,
            OutcomeId: "accepted",
            ViewersDelta: 2,
            DramaDelta: -1,
            FlagChanges: [],
            TerminalReasonCode: null);
    }

    private static EventResolution AnsweredEvent(string eventId, int index)
    {
        return new EventResolution(eventId, index, Answer());
    }
}
```

- [ ] **Step 3: Add a validator that accepts everything**

Create `src/Domain/Runs/RunStateValidator.cs` with this temporary skeleton, so the tests compile and show which rules are missing:

```csharp
using PolskiStreamerSymulatorApp.Domain.Catalog;

namespace PolskiStreamerSymulatorApp.Domain.Runs;

/// <summary>
/// Temporary skeleton: accepts every state, so the tests can show which rules are still missing.
/// </summary>
public static class RunStateValidator
{
    public static RunStateValidation Validate(RunState state, GameParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(parameters);
        return RunStateValidation.Success(new ValidatedRunState(state, parameters));
    }
}
```

- [ ] **Step 4: Run the tests and see the rules fail**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj`

Expected: `total: 109`, `failed: 37`, `succeeded: 72`. The five valid fixtures and `EveryErrorCodeHasAMutation` pass; the 33 mutation cases and the four other validator tests fail.

- [ ] **Step 5: Implement the validator**

Replace the whole content of `src/Domain/Runs/RunStateValidator.cs` with:

```csharp
using PolskiStreamerSymulatorApp.Domain.Catalog;
using PolskiStreamerSymulatorApp.Domain.Identity;
using PolskiStreamerSymulatorApp.Domain.Ledger;
using PolskiStreamerSymulatorApp.Domain.Randomness;
using PolskiStreamerSymulatorApp.Domain.Versioning;

namespace PolskiStreamerSymulatorApp.Domain.Runs;

/// <summary>
/// Checks an untrusted run state against the parameters of its pinned catalogue version before any game rule may use it.
/// It checks totals and structure, not a full replay of every historical step.
/// Every list and every non-nullable member must be non-null; the strict JSON contract guarantees that before mapping.
/// </summary>
public static class RunStateValidator
{
    public const int MaxRoll = 9_999;
    public const int MaxDrama = 100;

    public static RunStateValidation Validate(RunState state, GameParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(parameters);

        if (GameParametersValidator.Validate(parameters).Count > 0)
        {
            return RunStateValidation.Failure([new RunStateError(RunStateErrorCodes.InvalidGameParameters, "parameters")]);
        }

        List<RunStateError> errors = [];
        ValidateIdentity(state, errors);
        ValidateStatistics(state, parameters, errors);
        ValidateStatusAndEnding(state, parameters, errors);
        List<WeekView> weeks = CollectWeeks(state, errors);
        foreach (WeekView week in weeks)
        {
            ValidateWeek(state, parameters, week, errors);
        }

        ValidateLedger(state, weeks, errors);
        ValidateReconciliation(state, parameters, weeks, errors);
        ValidateFlags(state, errors);

        return errors.Count == 0
            ? RunStateValidation.Success(new ValidatedRunState(state, parameters))
            : RunStateValidation.Failure(errors);
    }

    private static void ValidateIdentity(RunState state, List<RunStateError> errors)
    {
        if (state.RulesVersion != RulesVersion.Current)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.UnsupportedRulesVersion, "rulesVersion"));
        }

        if (state.CatalogVersion < 1)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.InvalidCatalogVersion, "catalogVersion"));
        }

        if (state.RunId == Guid.Empty)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.InvalidRunId, "runId"));
        }

        if (!StreamerName.IsNormalizedValid(state.StreamerName))
        {
            errors.Add(new RunStateError(RunStateErrorCodes.InvalidStreamerName, "streamerName"));
        }

        if (!string.Equals(state.Rng.Algorithm, Pcg32.AlgorithmId, StringComparison.Ordinal))
        {
            errors.Add(new RunStateError(RunStateErrorCodes.UnsupportedRngAlgorithm, "rng.algorithm"));
        }
    }

    private static void ValidateStatistics(RunState state, GameParameters parameters, List<RunStateError> errors)
    {
        if (state.Week < 1 || state.Week > parameters.RunLengthWeeks)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.WeekOutOfRange, "week"));
        }

        if (state.Viewers < 0)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.ViewersNegative, "viewers"));
        }

        if (state.Drama is < 0 or > MaxDrama)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.DramaOutOfRange, "drama"));
        }
    }

    private static void ValidateStatusAndEnding(RunState state, GameParameters parameters, List<RunStateError> errors)
    {
        RunEndingKind? expectedEnding = state.Status switch
        {
            RunStatus.Completed => RunEndingKind.Completed,
            RunStatus.Bankrupt => RunEndingKind.Bankrupt,
            RunStatus.SpecialEnding => RunEndingKind.Special,
            _ => null,
        };
        bool consistent = state.Status switch
        {
            RunStatus.Active => state.CurrentWeek is null && state.Ending is null,
            RunStatus.PendingWeek => state.CurrentWeek is not null && state.Ending is null,
            RunStatus.Completed or RunStatus.Bankrupt or RunStatus.SpecialEnding =>
                state.CurrentWeek is null && state.Ending is not null && state.Ending.Kind == expectedEnding,
            _ => false,
        };
        if (!consistent)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.StatusInconsistent, "status"));
        }

        if (state.Ending is { } ending)
        {
            if (ending.Week != state.Week || (ending.Kind == RunEndingKind.Completed && ending.Week != parameters.RunLengthWeeks))
            {
                errors.Add(new RunStateError(RunStateErrorCodes.EndingInconsistent, "ending.week"));
            }

            if (ending.Kind != RunEndingKind.Special)
            {
                if (ending.ReasonCode is not null)
                {
                    errors.Add(new RunStateError(RunStateErrorCodes.EndingInconsistent, "ending.reasonCode"));
                }
            }
            else if (ending.ReasonCode is null)
            {
                errors.Add(new RunStateError(RunStateErrorCodes.EndingInconsistent, "ending.reasonCode"));
            }
            else
            {
                ValidateId(ending.ReasonCode, "ending.reasonCode", errors);
            }
        }

        bool atOrBelowThreshold = state.MoneyPln <= parameters.BankruptcyThresholdPln;
        if (state.Status == RunStatus.Bankrupt && !atOrBelowThreshold)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.BankruptMoneyAboveThreshold, "moneyPln"));
        }
        else if (state.Status != RunStatus.Bankrupt && atOrBelowThreshold)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.MoneyAtOrBelowBankruptcyThreshold, "moneyPln"));
        }
    }

    private static List<WeekView> CollectWeeks(RunState state, List<RunStateError> errors)
    {
        bool terminal = state.Status is RunStatus.Completed or RunStatus.Bankrupt or RunStatus.SpecialEnding;
        int expectedHistoryCount = terminal ? state.Week : state.Week - 1;
        if (state.History.Count != expectedHistoryCount)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.HistoryInconsistent, "history"));
        }

        List<WeekView> weeks = [];
        for (int i = 0; i < state.History.Count; i++)
        {
            WeekRecord record = state.History[i];
            string path = $"history[{i}]";
            if (record.Week != i + 1)
            {
                errors.Add(new RunStateError(RunStateErrorCodes.HistoryInconsistent, $"{path}.week"));
            }

            bool isFinal = terminal && i == state.History.Count - 1;
            weeks.Add(new WeekView(record.Week, path, record.Action, record.EncounterRolls, record.Events, $"{path}.events", null, isFinal));
        }

        if (state.CurrentWeek is { } current)
        {
            if (current.Week != state.Week)
            {
                errors.Add(new RunStateError(RunStateErrorCodes.HistoryInconsistent, "currentWeek.week"));
            }

            weeks.Add(new WeekView(
                current.Week,
                "currentWeek",
                current.Action,
                current.EncounterRolls,
                current.ResolvedEvents,
                "currentWeek.resolvedEvents",
                current.Pending,
                IsFinal: false));
        }

        return weeks;
    }

    private static void ValidateWeek(RunState state, GameParameters parameters, WeekView week, List<RunStateError> errors)
    {
        string actionPath = $"{week.Path}.action";
        ValidateId(week.Action.ActionId, $"{actionPath}.actionId", errors);
        ValidateId(week.Action.OutcomeId, $"{actionPath}.outcomeId", errors);
        ValidateRoll(week.Action.Roll, $"{actionPath}.roll", errors);

        HashSet<string> passing = ValidateEncounterRolls(week, errors);
        HashSet<string> selected = new(StringComparer.Ordinal);
        for (int i = 0; i < week.Events.Count; i++)
        {
            EventResolution resolution = week.Events[i];
            string path = $"{week.EventsPath}[{i}]";
            bool isLastEvent = i == week.Events.Count - 1;
            ValidateSelection(resolution.EventId, resolution.Index, i + 1, path, passing, selected, errors);
            if (resolution.Response is null)
            {
                bool bankruptAtEncounter = week.IsFinal && isLastEvent && state.Status == RunStatus.Bankrupt;
                if (!bankruptAtEncounter)
                {
                    errors.Add(new RunStateError(RunStateErrorCodes.UnansweredEvent, path));
                }
            }
            else
            {
                ValidateResponse(state, week, resolution.Response, isLastEvent, $"{path}.response", errors);
            }
        }

        if (week.Pending is { } pending)
        {
            ValidateSelection(pending.EventId, pending.Index, week.Events.Count + 1, $"{week.Path}.pending", passing, selected, errors);
        }

        int eventCount = week.Events.Count + (week.Pending is null ? 0 : 1);
        if (eventCount > parameters.MaxEventsPerWeek)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.EventCapExceeded, week.Path));
        }

        if (week.IsFinal && state.Status == RunStatus.SpecialEnding)
        {
            string? finalReason = week.Events.Count > 0 ? week.Events[^1].Response?.TerminalReasonCode : null;
            if (finalReason is null || !string.Equals(finalReason, state.Ending?.ReasonCode, StringComparison.Ordinal))
            {
                errors.Add(new RunStateError(RunStateErrorCodes.EndingInconsistent, "ending.reasonCode"));
            }
        }
    }

    private static HashSet<string> ValidateEncounterRolls(WeekView week, List<RunStateError> errors)
    {
        HashSet<string> rolled = new(StringComparer.Ordinal);
        HashSet<string> passing = new(StringComparer.Ordinal);
        for (int i = 0; i < week.EncounterRolls.Count; i++)
        {
            EncounterRoll roll = week.EncounterRolls[i];
            string path = $"{week.Path}.encounterRolls[{i}]";
            ValidateId(roll.EventId, $"{path}.eventId", errors);
            ValidateRoll(roll.Roll, $"{path}.roll", errors);
            if (!rolled.Add(roll.EventId))
            {
                errors.Add(new RunStateError(RunStateErrorCodes.EncounterRolledTwice, path));
            }
            else if (roll.Passed)
            {
                passing.Add(roll.EventId);
            }
        }

        return passing;
    }

    private static void ValidateSelection(
        string eventId,
        int index,
        int expectedIndex,
        string path,
        HashSet<string> passing,
        HashSet<string> selected,
        List<RunStateError> errors)
    {
        ValidateId(eventId, $"{path}.eventId", errors);
        if (index != expectedIndex)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.EventIndexInconsistent, $"{path}.index"));
        }

        if (!selected.Add(eventId))
        {
            errors.Add(new RunStateError(RunStateErrorCodes.EventSelectedTwice, $"{path}.eventId"));
        }

        if (!passing.Contains(eventId))
        {
            errors.Add(new RunStateError(RunStateErrorCodes.SelectedEventWithoutPassingRoll, $"{path}.eventId"));
        }
    }

    private static void ValidateResponse(
        RunState state,
        WeekView week,
        ResponseResolution response,
        bool isLastEvent,
        string path,
        List<RunStateError> errors)
    {
        ValidateId(response.OptionId, $"{path}.optionId", errors);
        ValidateId(response.OutcomeId, $"{path}.outcomeId", errors);
        ValidateRoll(response.Roll, $"{path}.roll", errors);
        for (int i = 0; i < response.FlagChanges.Count; i++)
        {
            ValidateId(response.FlagChanges[i].FlagId, $"{path}.flagChanges[{i}].flagId", errors);
        }

        if (response.TerminalReasonCode is null)
        {
            return;
        }

        ValidateId(response.TerminalReasonCode, $"{path}.terminalReasonCode", errors);
        bool mayEndRun = week.IsFinal && isLastEvent && (state.Status is RunStatus.Bankrupt or RunStatus.SpecialEnding);
        if (!mayEndRun)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.TerminalReasonNotApplied, $"{path}.terminalReasonCode"));
        }
    }

    private static void ValidateLedger(RunState state, List<WeekView> weeks, List<RunStateError> errors)
    {
        Dictionary<int, WeekView> weeksByNumber = new();
        foreach (WeekView week in weeks)
        {
            weeksByNumber.TryAdd(week.Week, week);
        }

        Dictionary<int, int> subscriptionsByWeek = new();
        HashSet<(int Week, int Step, CashFlowSource Source, int Ordinal)> keys = new();
        for (int i = 0; i < state.Ledger.Count; i++)
        {
            CashFlowEntry entry = state.Ledger[i];
            string path = $"ledger[{i}]";
            if (!CategoryMatchesSource(entry))
            {
                errors.Add(new RunStateError(RunStateErrorCodes.LedgerCategoryMismatch, $"{path}.category"));
            }

            if (!AmountAndOrdinalValid(entry))
            {
                errors.Add(new RunStateError(RunStateErrorCodes.LedgerAmountInvalid, path));
            }

            if (!keys.Add((entry.Week, entry.Step, entry.Source, entry.Ordinal)))
            {
                errors.Add(new RunStateError(RunStateErrorCodes.LedgerDuplicateEntry, path));
            }

            if (!weeksByNumber.TryGetValue(entry.Week, out WeekView? week))
            {
                errors.Add(new RunStateError(RunStateErrorCodes.LedgerWeekOutOfRange, $"{path}.week"));
                continue;
            }

            if (!StepMatchesSource(entry, week))
            {
                errors.Add(new RunStateError(RunStateErrorCodes.LedgerStepInvalid, $"{path}.step"));
            }

            if (entry.Source == CashFlowSource.Subscription)
            {
                subscriptionsByWeek[entry.Week] = subscriptionsByWeek.GetValueOrDefault(entry.Week) + 1;
            }
        }

        foreach (WeekView week in weeks)
        {
            if (subscriptionsByWeek.GetValueOrDefault(week.Week) != 1)
            {
                errors.Add(new RunStateError(RunStateErrorCodes.SubscriptionEntryCount, week.Path));
            }
        }
    }

    private static bool CategoryMatchesSource(CashFlowEntry entry)
    {
        return entry.Source switch
        {
            CashFlowSource.WeeklyActionCost or CashFlowSource.EncounterCost or CashFlowSource.ResponseCost =>
                entry.Category == CashFlowCategory.Expenses,
            CashFlowSource.Subscription => entry.Category == CashFlowCategory.Subscriptions,
            CashFlowSource.WeeklyActionOutcome or CashFlowSource.ResponseOutcome =>
                entry.Category is CashFlowCategory.Sponsors or CashFlowCategory.Donations or CashFlowCategory.Expenses,
            _ => false,
        };
    }

    private static bool AmountAndOrdinalValid(CashFlowEntry entry)
    {
        return entry.Source switch
        {
            CashFlowSource.Subscription => entry.AmountPln >= 0 && entry.Ordinal == 0,
            CashFlowSource.WeeklyActionCost or CashFlowSource.EncounterCost or CashFlowSource.ResponseCost =>
                entry.AmountPln > 0 && entry.Ordinal == 0,
            _ => entry.AmountPln > 0 && entry.Ordinal >= 0,
        };
    }

    private static bool StepMatchesSource(CashFlowEntry entry, WeekView week)
    {
        return entry.Source switch
        {
            CashFlowSource.WeeklyActionCost or CashFlowSource.WeeklyActionOutcome or CashFlowSource.Subscription => entry.Step == 0,
            CashFlowSource.EncounterCost =>
                entry.Step >= 1 && (EventAt(week, entry.Step) is not null || week.Pending?.Index == entry.Step),
            CashFlowSource.ResponseCost or CashFlowSource.ResponseOutcome => EventAt(week, entry.Step)?.Response is not null,
            _ => false,
        };
    }

    private static EventResolution? EventAt(WeekView week, int step)
    {
        return step >= 1 && step <= week.Events.Count ? week.Events[step - 1] : null;
    }

    private static void ValidateReconciliation(RunState state, GameParameters parameters, List<WeekView> weeks, List<RunStateError> errors)
    {
        Int128 money = parameters.StartingMoneyPln;
        foreach (CashFlowEntry entry in state.Ledger)
        {
            money += entry.SignedAmountPln;
        }

        if (money != state.MoneyPln)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.MoneyNotReconciled, "moneyPln"));
        }

        long viewers = parameters.StartingViewers;
        long drama = parameters.StartingDrama;
        foreach (WeekView week in weeks)
        {
            viewers += week.Action.ViewersDelta;
            drama += week.Action.DramaDelta;
            foreach (EventResolution resolution in week.Events)
            {
                if (resolution.Response is { } response)
                {
                    viewers += response.ViewersDelta;
                    drama += response.DramaDelta;
                }
            }
        }

        if (viewers != state.Viewers)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.ViewersNotReconciled, "viewers"));
        }

        if (drama != state.Drama)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.DramaNotReconciled, "drama"));
        }
    }

    private static void ValidateFlags(RunState state, List<RunStateError> errors)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        for (int i = 0; i < state.Flags.Count; i++)
        {
            NarrativeFlag flag = state.Flags[i];
            string path = $"flags[{i}]";
            ValidateId(flag.FlagId, $"{path}.flagId", errors);
            if (!seen.Add(flag.FlagId) || flag.SetWeek < 1 || flag.SetWeek > state.Week)
            {
                errors.Add(new RunStateError(RunStateErrorCodes.FlagInvalid, path));
            }
        }
    }

    private static void ValidateId(string value, string path, List<RunStateError> errors)
    {
        if (!StableId.IsValid(value))
        {
            errors.Add(new RunStateError(RunStateErrorCodes.InvalidStableId, path));
        }
    }

    private static void ValidateRoll(int roll, string path, List<RunStateError> errors)
    {
        if (roll is < 0 or > MaxRoll)
        {
            errors.Add(new RunStateError(RunStateErrorCodes.RollOutOfRange, path));
        }
    }

    private sealed record WeekView(
        int Week,
        string Path,
        ActionResolution Action,
        IReadOnlyList<EncounterRoll> EncounterRolls,
        IReadOnlyList<EventResolution> Events,
        string EventsPath,
        PendingEvent? Pending,
        bool IsFinal);
}
```

- [ ] **Step 6: Run the tests and see them pass**

Run: `dotnet test --project tests/Domain.Tests/Domain.Tests.csproj`

Expected: `Passed!` with `total: 109` and `failed: 0`.

- [ ] **Step 7: Commit**

```bash
cd .. && git add PolskiStreamerSymulatorApp/src/Domain/Versioning PolskiStreamerSymulatorApp/src/Domain/Runs PolskiStreamerSymulatorApp/tests/Domain.Tests/Runs && git commit -m "Add the run state and its validator" -m "<your Co-Authored-By trailer>"
```

---

### Task 5: Wire contracts and their JSON format

**Files:**
- Create: `PolskiStreamerSymulatorApp/src/Contracts/Serialization/StrictEnumConverter.cs`, `HexUInt64JsonConverter.cs`, `NullElementGuard.cs`, `ContractsJsonContext.cs`
- Create: `PolskiStreamerSymulatorApp/src/Contracts/Runs/RunStateDto.cs`, `WeekRecordDtos.cs`, `CashFlowEntryDto.cs`, `RunRequests.cs`
- Create: `PolskiStreamerSymulatorApp/src/Contracts/Setup/SetupContracts.cs`, `PolskiStreamerSymulatorApp/src/Contracts/Problems/ProblemCodes.cs`, `PolskiStreamerSymulatorApp/src/Contracts/Localization/SupportedLocales.cs`
- Create: `PolskiStreamerSymulatorApp/tests/Server.IntegrationTests/Contracts/ContractFixtures.cs`, `ContractJsonTests.cs`, `ContractConstantsTests.cs`

**Interfaces:**
- Consumes: nothing from Domain; Contracts must not reference Domain. The DTO shapes mirror Task 4's records.
- Produces: in `PolskiStreamerSymulatorApp.Contracts.Runs`, the DTO records and enums below, `PlanWeekRequest(RunStateDto State, string ActionId)`, `ChooseEventOptionRequest(RunStateDto State, string OptionId)`, and `SaveSchema.CurrentVersion = 1`; in `PolskiStreamerSymulatorApp.Contracts.Setup`, `SetupOptionsResponse`, `StreamerNameSuggestionResponse`, `StartRunRequest`; `ProblemCodes` with `All`; `SupportedLocales` with `Polish`, `English`, `Default`, `All`, `IsSupported`; and `ContractsJsonContext.Default` in `PolskiStreamerSymulatorApp.Contracts.Serialization`.

- [ ] **Step 1: Write the failing wire format tests**

Create `tests/Server.IntegrationTests/Contracts/ContractFixtures.cs`:

```csharp
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
```

Create `tests/Server.IntegrationTests/Contracts/ContractJsonTests.cs`. The `PendingWeekJson` constant is the exact serializer output for the pending fixture and must stay on one line:

```csharp
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

    private static void AssertWireName<T>(T value, string name, JsonTypeInfo<T> typeInfo)
    {
        string json = $"\"{name}\"";

        Assert.Equal(json, JsonSerializer.Serialize(value, typeInfo));
        Assert.Equal(value, JsonSerializer.Deserialize(json, typeInfo));
    }
}
```

Create `tests/Server.IntegrationTests/Contracts/ContractConstantsTests.cs`:

```csharp
using PolskiStreamerSymulatorApp.Contracts.Localization;
using PolskiStreamerSymulatorApp.Contracts.Problems;

namespace PolskiStreamerSymulatorApp.Server.IntegrationTests.Contracts;

public sealed class ContractConstantsTests
{
    [Theory]
    [InlineData("pl-PL", true)]
    [InlineData("en", true)]
    [InlineData("pl-pl", false)]
    [InlineData("en-US", false)]
    [InlineData("de", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void OnlyTheShippedLocalesAreSupported(string? locale, bool supported)
    {
        Assert.Equal(supported, SupportedLocales.IsSupported(locale));
    }

    [Fact]
    public void PolishIsTheDefaultLocale()
    {
        Assert.Equal(SupportedLocales.Polish, SupportedLocales.Default);
        Assert.Equal(new[] { "pl-PL", "en" }, SupportedLocales.All);
    }

    [Fact]
    public void ProblemCodesAreDistinctSnakeCaseValues()
    {
        Assert.Equal(ProblemCodes.All.Count, ProblemCodes.All.Distinct(StringComparer.Ordinal).Count());
        Assert.All(ProblemCodes.All, static code => Assert.Matches("^[a-z]+(_[a-z]+)*$", code));
    }
}
```

- [ ] **Step 2: Run the tests and see them fail**

Run: `dotnet test --project tests/Server.IntegrationTests/Server.IntegrationTests.csproj`

Expected: the build fails with error CS0246, because the DTOs and `ContractsJsonContext` do not exist yet.

- [ ] **Step 3: Implement the serialization helpers**

Create `src/Contracts/Serialization/StrictEnumConverter.cs`:

```csharp
using System.Text.Json.Serialization;

namespace PolskiStreamerSymulatorApp.Contracts.Serialization;

/// <summary>
/// Reads and writes an enum only by the fixed wire name each member declares; numbers and unknown names are rejected.
/// </summary>
public sealed class StrictEnumConverter<TEnum>() : JsonStringEnumConverter<TEnum>(namingPolicy: null, allowIntegerValues: false)
    where TEnum : struct, Enum;
```

Create `src/Contracts/Serialization/HexUInt64JsonConverter.cs`:

```csharp
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolskiStreamerSymulatorApp.Contracts.Serialization;

/// <summary>
/// Writes 64-bit generator values as exactly 16 lowercase hexadecimal digits, because JSON numbers above 2^53 lose precision in JavaScript.
/// </summary>
public sealed class HexUInt64JsonConverter : JsonConverter<ulong>
{
    private const int Digits = 16;

    public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? text = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
        if (text is null
            || text.Length != Digits
            || !text.All(IsLowercaseHexDigit)
            || !ulong.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ulong value))
        {
            throw new JsonException($"Expected {Digits} lowercase hexadecimal digits.");
        }

        return value;
    }

    public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.ToString("x16", CultureInfo.InvariantCulture));
    }

    private static bool IsLowercaseHexDigit(char character)
    {
        return character is (>= '0' and <= '9') or (>= 'a' and <= 'f');
    }
}
```

Create `src/Contracts/Serialization/NullElementGuard.cs`:

```csharp
using System.Text.Json;

namespace PolskiStreamerSymulatorApp.Contracts.Serialization;

/// <summary>
/// Rejects null array elements after deserialization, because nullable annotations do not cover collection elements.
/// </summary>
internal static class NullElementGuard
{
    public static void ThrowIfAnyNull<T>(IReadOnlyList<T> items, string propertyName)
        where T : class
    {
        foreach (T? item in items)
        {
            if (item is null)
            {
                throw new JsonException($"The JSON array '{propertyName}' must not contain null.");
            }
        }
    }
}
```

- [ ] **Step 4: Implement the DTOs, requests, and constants**

Create `src/Contracts/Runs/RunStateDto.cs`:

```csharp
using System.Text.Json.Serialization;
using PolskiStreamerSymulatorApp.Contracts.Serialization;

namespace PolskiStreamerSymulatorApp.Contracts.Runs;

/// <summary>
/// A career as saved in the browser and exchanged with the server: the domain run state plus the save schema version.
/// </summary>
public sealed record RunStateDto(
    int SchemaVersion,
    int RulesVersion,
    int CatalogVersion,
    Guid RunId,
    string StreamerName,
    int Week,
    RunStatusDto Status,
    long MoneyPln,
    int Viewers,
    int Drama,
    RngStateDto Rng,
    IReadOnlyList<CashFlowEntryDto> Ledger,
    IReadOnlyList<WeekRecordDto> History,
    WeekInProgressDto? CurrentWeek,
    IReadOnlyList<NarrativeFlagDto> Flags,
    RunEndingDto? Ending) : IJsonOnDeserialized
{
    void IJsonOnDeserialized.OnDeserialized()
    {
        NullElementGuard.ThrowIfAnyNull(Ledger, "ledger");
        NullElementGuard.ThrowIfAnyNull(History, "history");
        NullElementGuard.ThrowIfAnyNull(Flags, "flags");
    }
}

[JsonConverter(typeof(StrictEnumConverter<RunStatusDto>))]
public enum RunStatusDto
{
    [JsonStringEnumMemberName("active")]
    Active,

    [JsonStringEnumMemberName("pendingWeek")]
    PendingWeek,

    [JsonStringEnumMemberName("completed")]
    Completed,

    [JsonStringEnumMemberName("bankrupt")]
    Bankrupt,

    [JsonStringEnumMemberName("specialEnding")]
    SpecialEnding,
}

/// <summary>
/// The saved random generator position. The three numbers travel as 16-digit hexadecimal strings.
/// </summary>
public sealed record RngStateDto(
    string Algorithm,
    [property: JsonConverter(typeof(HexUInt64JsonConverter))] ulong Seed,
    [property: JsonConverter(typeof(HexUInt64JsonConverter))] ulong Stream,
    [property: JsonConverter(typeof(HexUInt64JsonConverter))] ulong State);

public sealed record NarrativeFlagDto(string FlagId, int SetWeek);

public sealed record RunEndingDto(RunEndingKindDto Kind, int Week, string? ReasonCode);

[JsonConverter(typeof(StrictEnumConverter<RunEndingKindDto>))]
public enum RunEndingKindDto
{
    [JsonStringEnumMemberName("completed")]
    Completed,

    [JsonStringEnumMemberName("bankrupt")]
    Bankrupt,

    [JsonStringEnumMemberName("special")]
    Special,
}
```

Create `src/Contracts/Runs/WeekRecordDtos.cs`:

```csharp
using System.Text.Json.Serialization;
using PolskiStreamerSymulatorApp.Contracts.Serialization;

namespace PolskiStreamerSymulatorApp.Contracts.Runs;

public sealed record WeekRecordDto(
    int Week,
    ActionResolutionDto Action,
    IReadOnlyList<EncounterRollDto> EncounterRolls,
    IReadOnlyList<EventResolutionDto> Events) : IJsonOnDeserialized
{
    void IJsonOnDeserialized.OnDeserialized()
    {
        NullElementGuard.ThrowIfAnyNull(EncounterRolls, "encounterRolls");
        NullElementGuard.ThrowIfAnyNull(Events, "events");
    }
}

public sealed record WeekInProgressDto(
    int Week,
    ActionResolutionDto Action,
    IReadOnlyList<EncounterRollDto> EncounterRolls,
    IReadOnlyList<EventResolutionDto> ResolvedEvents,
    PendingEventDto Pending) : IJsonOnDeserialized
{
    void IJsonOnDeserialized.OnDeserialized()
    {
        NullElementGuard.ThrowIfAnyNull(EncounterRolls, "encounterRolls");
        NullElementGuard.ThrowIfAnyNull(ResolvedEvents, "resolvedEvents");
    }
}

public sealed record ActionResolutionDto(string ActionId, int Roll, string OutcomeId, int ViewersDelta, int DramaDelta);

public sealed record EncounterRollDto(string EventId, int Roll, bool Passed);

public sealed record PendingEventDto(string EventId, int Index);

public sealed record EventResolutionDto(string EventId, int Index, ResponseResolutionDto? Response);

public sealed record ResponseResolutionDto(
    string OptionId,
    int Roll,
    string OutcomeId,
    int ViewersDelta,
    int DramaDelta,
    IReadOnlyList<FlagChangeDto> FlagChanges,
    string? TerminalReasonCode) : IJsonOnDeserialized
{
    void IJsonOnDeserialized.OnDeserialized()
    {
        NullElementGuard.ThrowIfAnyNull(FlagChanges, "flagChanges");
    }
}

public sealed record FlagChangeDto(string FlagId, FlagOperationDto Operation);

[JsonConverter(typeof(StrictEnumConverter<FlagOperationDto>))]
public enum FlagOperationDto
{
    [JsonStringEnumMemberName("set")]
    Set,

    [JsonStringEnumMemberName("clear")]
    Clear,
}
```

Create `src/Contracts/Runs/CashFlowEntryDto.cs`:

```csharp
using System.Text.Json.Serialization;
using PolskiStreamerSymulatorApp.Contracts.Serialization;

namespace PolskiStreamerSymulatorApp.Contracts.Runs;

public sealed record CashFlowEntryDto(
    int Week,
    int Step,
    CashFlowSourceDto Source,
    int Ordinal,
    CashFlowCategoryDto Category,
    long AmountPln);

[JsonConverter(typeof(StrictEnumConverter<CashFlowSourceDto>))]
public enum CashFlowSourceDto
{
    [JsonStringEnumMemberName("weeklyActionCost")]
    WeeklyActionCost,

    [JsonStringEnumMemberName("weeklyActionOutcome")]
    WeeklyActionOutcome,

    [JsonStringEnumMemberName("subscription")]
    Subscription,

    [JsonStringEnumMemberName("encounterCost")]
    EncounterCost,

    [JsonStringEnumMemberName("responseCost")]
    ResponseCost,

    [JsonStringEnumMemberName("responseOutcome")]
    ResponseOutcome,
}

[JsonConverter(typeof(StrictEnumConverter<CashFlowCategoryDto>))]
public enum CashFlowCategoryDto
{
    [JsonStringEnumMemberName("sponsors")]
    Sponsors,

    [JsonStringEnumMemberName("donations")]
    Donations,

    [JsonStringEnumMemberName("subscriptions")]
    Subscriptions,

    [JsonStringEnumMemberName("expenses")]
    Expenses,
}
```

Create `src/Contracts/Runs/RunRequests.cs`:

```csharp
namespace PolskiStreamerSymulatorApp.Contracts.Runs;

/// <summary>
/// Plays the weekly action of the current week. The server answers with the resulting RunStateDto.
/// </summary>
public sealed record PlanWeekRequest(RunStateDto State, string ActionId);

/// <summary>
/// Answers the pending event. The server answers with the resulting RunStateDto.
/// </summary>
public sealed record ChooseEventOptionRequest(RunStateDto State, string OptionId);

/// <summary>
/// Version of the JSON shape of saves and run payloads. Any shape change raises it.
/// </summary>
public static class SaveSchema
{
    public const int CurrentVersion = 1;
}
```

Create `src/Contracts/Setup/SetupContracts.cs`:

```csharp
namespace PolskiStreamerSymulatorApp.Contracts.Setup;

/// <summary>
/// What the setup screen shows before a run starts, read from one published catalogue version.
/// </summary>
public sealed record SetupOptionsResponse(
    int CatalogVersion,
    int RunLengthWeeks,
    long StartingMoneyPln,
    int StartingViewers,
    int StartingDrama,
    long BankruptcyThresholdPln,
    string SuggestedStreamerName);

public sealed record StreamerNameSuggestionResponse(int CatalogVersion, string StreamerName);

/// <summary>
/// Starts a run on the catalogue version shown during setup. The server answers with the new RunStateDto.
/// </summary>
public sealed record StartRunRequest(int CatalogVersion, string StreamerName);
```

Create `src/Contracts/Problems/ProblemCodes.cs`:

```csharp
namespace PolskiStreamerSymulatorApp.Contracts.Problems;

/// <summary>
/// Stable error codes the API returns in Problem Details responses.
/// </summary>
public static class ProblemCodes
{
    public const string InvalidRequest = "invalid_request";
    public const string InvalidState = "invalid_state";
    public const string InvalidChoice = "invalid_choice";
    public const string InvalidStreamerName = "invalid_streamer_name";
    public const string IncompatibleSave = "incompatible_save";
    public const string CatalogVersionUnavailable = "catalog_version_unavailable";
    public const string RunFinished = "run_finished";

    public static IReadOnlyList<string> All { get; } =
    [
        InvalidRequest,
        InvalidState,
        InvalidChoice,
        InvalidStreamerName,
        IncompatibleSave,
        CatalogVersionUnavailable,
        RunFinished,
    ];
}
```

Create `src/Contracts/Localization/SupportedLocales.cs`:

```csharp
namespace PolskiStreamerSymulatorApp.Contracts.Localization;

/// <summary>
/// Locales the game ships. A locale is a query parameter for text and never part of a run state.
/// </summary>
public static class SupportedLocales
{
    public const string Polish = "pl-PL";
    public const string English = "en";
    public const string Default = Polish;

    public static IReadOnlyList<string> All { get; } = [Polish, English];

    public static bool IsSupported(string? locale)
    {
        return locale is Polish or English;
    }
}
```

- [ ] **Step 5: Implement the JSON context**

Create `src/Contracts/Serialization/ContractsJsonContext.cs`:

```csharp
using System.Text.Json.Serialization;
using PolskiStreamerSymulatorApp.Contracts.Runs;
using PolskiStreamerSymulatorApp.Contracts.Setup;

namespace PolskiStreamerSymulatorApp.Contracts.Serialization;

/// <summary>
/// The single JSON format of the contracts: camelCase names, every property required, nulls only where declared,
/// unknown properties rejected. Enum names and hexadecimal numbers come from converters declared on the types.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(RunStateDto))]
[JsonSerializable(typeof(PlanWeekRequest))]
[JsonSerializable(typeof(ChooseEventOptionRequest))]
[JsonSerializable(typeof(StartRunRequest))]
[JsonSerializable(typeof(SetupOptionsResponse))]
[JsonSerializable(typeof(StreamerNameSuggestionResponse))]
[JsonSerializable(typeof(RunStatusDto))]
[JsonSerializable(typeof(RunEndingKindDto))]
[JsonSerializable(typeof(FlagOperationDto))]
[JsonSerializable(typeof(CashFlowSourceDto))]
[JsonSerializable(typeof(CashFlowCategoryDto))]
public sealed partial class ContractsJsonContext : JsonSerializerContext;
```

- [ ] **Step 6: Run the tests and see them pass**

Run: `dotnet test --project tests/Server.IntegrationTests/Server.IntegrationTests.csproj`

Expected: `Passed!` with `total: 57` and `failed: 0`; the 11 earlier hosting and health tests still pass.

- [ ] **Step 7: Commit**

```bash
cd .. && git add PolskiStreamerSymulatorApp/src/Contracts PolskiStreamerSymulatorApp/tests/Server.IntegrationTests/Contracts && git commit -m "Add wire contracts and their strict JSON format" -m "<your Co-Authored-By trailer>"
```

---

### Task 6: Documentation and final verification

**Files:**
- Modify: `docs/technical-design.md`, `docs/balance.md`, `docs/streamer-name-generation.md`, `docs/testing-strategy.md`, `docs/decisions.md`, `docs/library-guide.md`, `docs/delivery-plan.md`

**Interfaces:**
- Consumes: the types and file locations from Tasks 1 to 5.
- Produces: documentation that matches the code.

- [ ] **Step 1: Technical design**

In `docs/technical-design.md`, replace the sentence

```text
The score is derived, not an additional mutable run statistic.
```

with

```text
The score is derived, not an additional mutable run statistic.

F2 implements this model in `Domain` (`Runs`, `Ledger`, `Randomness`, `Catalog`, `Identity`) and its wire format in `Contracts`. `RunStateValidator` checks a state against its pinned `GameParameters` and returns either stable error codes or a `ValidatedRunState`, the only form the game rules accept; it checks totals and structure rather than replaying history. The repeat-policy view of past encounters comes from the weekly history, and each ledger entry points to its week and step instead of copying IDs. Every week records exactly one subscription entry, even at 0 PLN. The gameplay generator is PCG32 (XSH-RR 64/32): a roll is its unbiased bounded draw from 0 to 9,999, and the save keeps the seed, stream, and current state. `ContractsJsonContext` defines the only JSON format: camelCase names, every property required, nulls only where declared and never inside arrays, unknown properties and numeric or unknown enum values rejected, fixed enum names, and 64-bit generator values as 16 lowercase hexadecimal digits. The design record is `docs/superpowers/specs/2026-10-06-f2-run-state-contracts-design.md`.
```

- [ ] **Step 2: Balance**

In `docs/balance.md`, replace

```text
4. Consume one value in `[0, 9999]` from the saved seeded PRNG.
```

with

```text
4. Consume one value in `[0, 9999]` from the saved seeded PRNG: the unbiased bounded draw of the PCG32 generator with bound 10,000.
```

and replace

```text
Each cash-flow entry has a category, positive magnitude in whole PLN, signed effect on money, week, and source action/event/outcome ID.
```

with

```text
Each cash-flow entry has a category, a magnitude in whole PLN, a signed effect on money, a week, and a step that points to the week's action or event record, which names the source action, event, and outcome. Magnitudes are positive, except that the weekly subscription entry is recorded even when it pays 0 PLN.
```

- [ ] **Step 3: Streamer name generation**

In `docs/streamer-name-generation.md`, replace

```text
Proposed validation for both suggested and custom names: normalize Unicode to NFC, trim outer whitespace, accept 2–32 displayed characters made of letters, digits, single internal spaces, hyphens, or underscores, and reject control characters.
```

with

```text
Validation for both suggested and custom names, implemented by `StreamerName` in Domain: normalize Unicode to NFC, trim outer whitespace, accept 2–32 displayed characters (text elements) made of letters, decimal digits, combining marks after a letter or digit, single internal spaces, hyphens, or underscores, require at least one letter or digit, allow at most 64 UTF-16 code units, and reject control characters. A saved name must already be in this normalized form.
```

- [ ] **Step 4: Testing strategy**

In `docs/testing-strategy.md`, replace the row

```text
| `tests/Domain.Tests` | `Domain` | Fast deterministic rules and property-style invariants |
```

with

```text
| `tests/Domain.Tests` | `Domain` | Random generator, streamer names, parameter and run-state validation (in place since F2); later fast deterministic rules and property-style invariants |
```

replace

```text
Health probe semantics and client hosting (in place since F1); later the stateless gameplay HTTP contract,
```

with

```text
Health probe semantics and client hosting (in place since F1) and the contracts' JSON wire format (since F2); later the stateless gameplay HTTP contract,
```

and replace

```text
`Server.IntegrationTests` exists since work package F1. Add the other projects with the first behavior they verify.
```

with

```text
`Server.IntegrationTests` exists since work package F1 and `Domain.Tests` since F2. Add the other projects with the first behavior they verify.
```

- [ ] **Step 5: Decisions**

In `docs/decisions.md`, replace

```text
Initial word lists and name-validation details in `streamer-name-generation.md` are proposed. |
```

with

```text
Initial word lists in `streamer-name-generation.md` are proposed; the name validation is implemented since F2. |
```

and add these two rows directly after the `D-60` row:

```text
| D-61 | Gameplay random generator | Confirmed | Every gameplay roll uses PCG32 (XSH-RR 64/32) as published in `pcg_basic.c`, with its unbiased bounded draw for values 0 to 9,999. `StartRun` seeds it from a cryptographic source, and the save keeps seed, stream, and current state. The generator belongs to `RulesVersion` 1. |
| D-62 | Run-state wire format | Confirmed | Saves and API payloads use one strict JSON format defined by `ContractsJsonContext` and versioned by `SaveSchema.CurrentVersion`, initially 1: every property required, nulls only where declared and never inside arrays, unknown properties and numeric enums rejected, fixed enum names, and 64-bit generator values as hexadecimal strings. Any shape change raises the schema version. |
```

- [ ] **Step 6: Library guide and delivery plan**

In `docs/library-guide.md`, replace

```text
API and versioned save serialization; avoid another JSON package without a concrete gap
```

with

```text
API and versioned save serialization through the source-generated `ContractsJsonContext`; avoid another JSON package without a concrete gap
```

In `docs/delivery-plan.md`, replace

```text
so F3 still owes the CI build and test gate.
```

with

```text
so F3 still owes the CI build and test gate. F2 was completed on 2026-10-07: Domain holds the run state, the PCG32 generator, the ledger, the pinned parameters, and the run-state validator, and Contracts holds the strict JSON wire format.
```

- [ ] **Step 7: Final verification**

Run:

```bash
dotnet build PolskiStreamerSymulatorApp.sln
dotnet test --solution PolskiStreamerSymulatorApp.sln -c Release
dotnet list src/Domain/Domain.csproj reference
dotnet list src/Contracts/Contracts.csproj reference
```

Expected: `0 Warning(s)` and `0 Error(s)`; `Passed!` with `total: 166` and `failed: 0`; Domain and Contracts each report no project references.

- [ ] **Step 8: Commit**

```bash
cd .. && git add docs/technical-design.md docs/balance.md docs/streamer-name-generation.md docs/testing-strategy.md docs/decisions.md docs/library-guide.md docs/delivery-plan.md && git commit -m "Document the F2 run state and wire format" -m "<your Co-Authored-By trailer>" && git status --short
```

Expected: the commit succeeds and `git status --short` prints nothing.
