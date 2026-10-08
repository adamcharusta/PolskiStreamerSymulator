using System.Diagnostics.CodeAnalysis;
using PolskiStreamerSymulatorApp.Domain.Runs;

namespace PolskiStreamerSymulatorApp.Domain.Engine;

/// <summary>
/// Outcome of planning a week: either the next validated run state or one stable error code from WeekPlanErrorCodes.
/// </summary>
public sealed class WeekPlanResult
{
    private WeekPlanResult(ValidatedRunState? value, string? errorCode)
    {
        Value = value;
        ErrorCode = errorCode;
    }

    public ValidatedRunState? Value { get; }

    public string? ErrorCode { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(ErrorCode))]
    public bool IsSuccess => Value is not null;

    internal static WeekPlanResult Success(ValidatedRunState value)
    {
        return new WeekPlanResult(value, null);
    }

    internal static WeekPlanResult Failure(string errorCode)
    {
        return new WeekPlanResult(null, errorCode);
    }
}
