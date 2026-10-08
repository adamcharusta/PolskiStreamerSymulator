namespace PolskiStreamerSymulatorApp.Domain.Catalog;

/// <summary>
/// The published content a run plays against: its version, its typed parameters, and its enabled weekly actions in catalogue order.
/// Untrusted until GameCatalogValidator turns it into a ValidatedCatalog.
/// </summary>
public sealed record GameCatalog(int Version, GameParameters Parameters, IReadOnlyList<WeeklyActionDefinition> WeeklyActions);
