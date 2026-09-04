using Winwright.Verdicts;

namespace Winwright.Tracing;

/// <summary>
/// One step, as the run saw it rather than as it concluded. A verdict with no record makes every
/// failure a re-run, and a re-run on another desk answers a different question — so what is kept
/// here is the observation: the locator as the scenario wrote it, what it resolved to, the pattern
/// that was used, what was read back, how long it waited and how many polls it took.
/// </summary>
public sealed record TraceStep
{
    /// <summary>
    /// Its ordinal in the run, counted from 1. Assigned by <see cref="TraceWriter"/> and never by
    /// a caller: a number typed at the call site is a number that goes wrong on the first retry.
    /// </summary>
    public int Step { get; init; }

    /// <summary>What was done — launch, click, read, assert.</summary>
    public required string Verb { get; init; }

    /// <summary>What the step addressed, spelled exactly as the scenario spells it.</summary>
    public required string Locator { get; init; }

    /// <summary>What the locator actually resolved to, or null where it resolved to nothing.</summary>
    public string? Resolved { get; init; }

    /// <summary>The UI Automation pattern the act went through, where one was used.</summary>
    public string? Pattern { get; init; }

    /// <summary>What was read back afterwards — the value that makes the verdict checkable.</summary>
    public string? ReadBack { get; init; }

    /// <summary>
    /// What the reading had settled to by the time the verdict was taken, where that is not what
    /// this step read. WW366.
    /// <para>
    /// An act reads once, the moment it returns; a step that also expects something polls after it
    /// to a deadline. Both readings are true and they are about different instants, so a trace line
    /// carrying only the first sits beside a verdict that turned on the second — and a reader whom
    /// the verdict surprised is sent to the control when the answer was the timing. That is the
    /// afternoon WW353 described, moved from the verdict to the page a person actually opens.
    /// </para>
    /// <para>
    /// Null where there is nothing to say, which is most lines: a step with no expectation, and a
    /// step whose expectation settled on what the act had already read. A second value on every
    /// line would be the mark that marks nothing.
    /// </para>
    /// <para>
    /// A difference is shown and never judged. It is the window settling after the act, or it is
    /// the step naming a reading the act's own is not — <c>toggle</c> against what the element
    /// offers first — and both are things a reader wants in front of them rather than things this
    /// line should decide between.
    /// </para>
    /// </summary>
    public string? Settled { get; init; }

    /// <summary>How long the step waited for the locator to resolve, in milliseconds.</summary>
    public int WaitedMs { get; init; }

    /// <summary>How many times it polled. Where it resolved at all, the last poll is the one that saw it.</summary>
    public int Polls { get; init; }

    /// <summary>
    /// How many attempts the act took. One by default, because every step was attempted once — and
    /// a green step that says three is a finding a green with no count would have thrown away.
    /// </summary>
    public int Attempts { get; init; } = 1;

    /// <summary>
    /// Where the value this step compared against came from — the file and line, or the element
    /// and pattern. Null where the step compared against nothing, which is most acts; present on
    /// every assert, because an expectation whose origin the trace does not record is one a reader
    /// audits by opening the file the run already read.
    /// </summary>
    public Provenance? From { get; init; }

    /// <summary>
    /// The assertion this step settled, where it settled one. The other half of the join: a
    /// reader who started from the trace reaches the summary line without matching prose, the way
    /// a reader who started from the summary reaches the step.
    /// </summary>
    public string? Asserted { get; init; }

    /// <summary>The reading this step earned.</summary>
    public required StepVerdict Verdict { get; init; }

    /// <summary>Why, on anything that is not <see cref="StepVerdict.Ok"/>: what broke, or what was absent.</summary>
    public string? Detail { get; init; }
}
