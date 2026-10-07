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
