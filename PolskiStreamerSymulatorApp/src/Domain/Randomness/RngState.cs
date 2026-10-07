namespace PolskiStreamerSymulatorApp.Domain.Randomness;

/// <summary>
/// Saved position of the gameplay random generator. Seed and stream identify the sequence; state is the current position.
/// </summary>
public sealed record RngState(string Algorithm, ulong Seed, ulong Stream, ulong State);
