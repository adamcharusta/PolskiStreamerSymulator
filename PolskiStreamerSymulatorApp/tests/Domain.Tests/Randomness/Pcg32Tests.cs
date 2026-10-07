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
