namespace PolskiStreamerSymulatorApp.Domain.Randomness;

/// <summary>
/// PCG32 (XSH-RR 64/32) as published in pcg_basic.c by Melissa O'Neill. This port follows that file, which is published under the Apache License 2.0 (or MIT); this note is attribution, not legal advice. Every gameplay roll comes from this generator.
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
