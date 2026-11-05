namespace Winwright.Verdicts;

/// <summary>
/// The harness itself broke: a pattern that threw, an assembly that would not load, a locator
/// that could not be parsed. None of these is a statement about the code under test, and
/// reporting one as a failed assertion sends whoever reads it to the wrong repository — so it
/// carries the step it came from and the exception, and it is its own outcome.
/// </summary>
public sealed record HarnessError
{
    private HarnessError(int step, string where, string exceptionType, string message, string? stackTrace)
    {
        Step = step;
        Where = where;
        ExceptionType = exceptionType;
        Message = message;
        StackTrace = stackTrace;
    }

    /// <summary>The step's ordinal in the run, counted from 1. Zero where it broke before any step.</summary>
    public int Step { get; }

    /// <summary>The step it came from, named the way the trace names it.</summary>
    public string Where { get; }

    /// <summary>The exception's type, which is what says whose defect this is.</summary>
    public string ExceptionType { get; }

    /// <summary>What it said.</summary>
    public string Message { get; }

    /// <summary>Where it came from, kept because the reader of this is opening the harness next.</summary>
    public string? StackTrace { get; }

    /// <summary>Record what was thrown, taking the type, the message and the trace off the exception.</summary>
    public static HarnessError At(int step, string where, Exception thrown)
    {
        ArgumentNullException.ThrowIfNull(thrown);
        return new HarnessError(
            Ordinal(step), Named(where), thrown.GetType().Name, thrown.Message, thrown.StackTrace);
    }

    /// <summary>Record one read back from a trace, where the exception object is long gone.</summary>
    public static HarnessError At(int step, string where, string exceptionType, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exceptionType);
        return new HarnessError(Ordinal(step), Named(where), exceptionType.Trim(), (message ?? "").Trim(), null);
    }

    /// <summary>The one line a summary shows: where it broke and what said so.</summary>
    public override string ToString() =>
        Step > 0
            ? $"[step {Step}] {Where} - {ExceptionType}: {Message}"
            : $"{Where} - {ExceptionType}: {Message}";

    private static int Ordinal(int step) =>
        step >= 0 ? step : throw new ArgumentOutOfRangeException(nameof(step), step, "a step's ordinal counts from 1");

    private static string Named(string where) =>
        string.IsNullOrWhiteSpace(where)
            ? throw new ArgumentException("a harness error names the step it came from", nameof(where))
            : where.Trim();
}
