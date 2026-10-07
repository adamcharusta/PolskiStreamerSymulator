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
