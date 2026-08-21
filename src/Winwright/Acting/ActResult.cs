using Winwright.Locating;
using Winwright.Tracing;

namespace Winwright.Acting;

/// <summary>
/// What one act did, with the element it addressed and what its patterns read either side of it.
/// The two readings are values taken at two instants, never one live view asked twice — which is
/// what makes "it changed" a claim that can be false.
/// </summary>
public sealed record ActResult
{
    internal ActResult(
        string verb,
        Locator locator,
        string pattern,
        ElementFacts element,
        PatternValues before,
        PatternValues after,
        int waitedMs,
        int polls)
    {
        Verb = verb;
        Locator = locator;
        Pattern = pattern;
        Element = element;
        Before = before;
        After = after;
        WaitedMs = waitedMs;
        Polls = polls;
    }

    /// <summary>What was done, as a scenario names it.</summary>
    public string Verb { get; }

    /// <summary>What it addressed.</summary>
    public Locator Locator { get; }

    /// <summary>The UI Automation pattern it went through.</summary>
    public string Pattern { get; }

    /// <summary>What the element was, read at the moment the act ran.</summary>
    public ElementFacts Element { get; }

    /// <summary>What its patterns read before.</summary>
    public PatternValues Before { get; }

    /// <summary>What they read after, re-resolved rather than re-asked of the same handle.</summary>
    public PatternValues After { get; }

    /// <summary>How long resolving it took.</summary>
    public int WaitedMs { get; }

    /// <summary>How many looks resolving it took.</summary>
    public int Polls { get; }

    /// <summary>
    /// Whether anything the element reports actually moved. False is a real answer and not an
    /// error: an act asked for is not an act that landed, and this is the difference.
    /// </summary>
    public bool Changed => Before != After;

    /// <summary>The step a trace records, with the read-back the act is checkable by.</summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = Verb,
        Locator = Locator.Text,
        Resolved = Element.ToString(),
        Pattern = Pattern,
        ReadBack = After.Reading(),
        WaitedMs = WaitedMs,
        Polls = Polls,
        Verdict = StepVerdict.Ok,
    };

    /// <summary>The one line a report shows, with what moved where it moved.</summary>
    public override string ToString()
    {
        var moved = Changed
            ? $"{Before.Reading() ?? "nothing"} -> {After.Reading() ?? "nothing"}"
            : "nothing it reports moved";

        return $"{Verb} {Element} via {Pattern}: {moved}";
    }
}
