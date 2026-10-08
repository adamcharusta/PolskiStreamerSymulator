using System.Diagnostics.CodeAnalysis;

namespace PolskiStreamerSymulatorApp.Domain.Catalog;

/// <summary>
/// Outcome of validating a catalogue: either errors or a catalogue the engine may use.
/// </summary>
public sealed class CatalogValidation
{
    private CatalogValidation(IReadOnlyList<CatalogError> errors, ValidatedCatalog? value)
    {
        Errors = errors;
        Value = value;
    }

    public IReadOnlyList<CatalogError> Errors { get; }

    public ValidatedCatalog? Value { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsValid => Value is not null;

    internal static CatalogValidation Success(ValidatedCatalog value)
    {
        return new CatalogValidation([], value);
    }

    internal static CatalogValidation Failure(IReadOnlyList<CatalogError> errors)
    {
        return new CatalogValidation(errors, null);
    }
}

/// <summary>
/// A catalogue that passed validation. Only GameCatalogValidator creates it, from copies of the caller's lists,
/// so later changes to those lists cannot change it.
/// </summary>
public sealed class ValidatedCatalog
{
    private readonly Dictionary<string, WeeklyActionDefinition> _actionsById;

    internal ValidatedCatalog(int version, GameParameters parameters, IReadOnlyList<WeeklyActionDefinition> weeklyActions)
    {
        Version = version;
        Parameters = parameters;
        WeeklyActions = weeklyActions;
        _actionsById = weeklyActions.ToDictionary(static action => action.ActionId, StringComparer.Ordinal);
    }

    public int Version { get; }

    public GameParameters Parameters { get; }

    /// <summary>
    /// The weekly actions in catalogue order.
    /// </summary>
    public IReadOnlyList<WeeklyActionDefinition> WeeklyActions { get; }

    public bool TryGetAction(string actionId, [NotNullWhen(true)] out WeeklyActionDefinition? action)
    {
        ArgumentNullException.ThrowIfNull(actionId);
        return _actionsById.TryGetValue(actionId, out action);
    }
}

/// <summary>
/// One catalogue validation failure: a stable code from CatalogErrorCodes and the path of the offending value.
/// </summary>
public sealed record CatalogError(string Code, string Path);
