using Winwright.Locating;
using Winwright.Tracing;
using Winwright.Verdicts;

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
        int polls,
        Precondition? needed = null)
    {
        Verb = verb;
        Locator = locator;
        Pattern = pattern;
        Element = element;
        Before = before;
        After = after;
        WaitedMs = waitedMs;
        Polls = polls;
        Needed = needed;
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
    /// What this act needed of the machine, where it needed anything of it.
    /// <para>
    /// WW225. Null on the eight acts that go through a control's own pattern, and that is not an
    /// omission: nothing about the desk stops a pattern act, which is why it is the default and why
    /// those eight were the whole vocabulary a case could name. A synthesised act needs the
    /// foreground, and the keyboard ones need the focus as well — so the answer to <em>did it
    /// change</em> has a second half, and without somewhere to put it the two collapse.
    /// </para>
    /// <para>
    /// Collapsing them is the failure this field exists to stop. An act that never ran because
    /// another window held the keyboard reads exactly like a control that would not move, and
    /// reporting the first as the second is a red about the application on a fact about the desk.
    /// Present and unsatisfied means nothing was attempted, so <see cref="Changed"/> being false says
    /// nothing at all.
    /// </para>
    /// </summary>
    public Precondition? Needed { get; }

    /// <summary>
    /// Whether this act was actually attempted. False only where something it needed was absent, so
    /// a reader never has to infer it from a reading that did not move.
    /// </summary>
    public bool Attempted => Needed is null || Needed.Satisfied;

    /// <summary>
    /// Whether anything the element reports actually moved. False is a real answer and not an
    /// error: an act asked for is not an act that landed, and this is the difference.
    /// </summary>
    public bool Changed => Before != After;

    /// <summary>
    /// The step a trace records, with the read-back the act is checkable by. An act that was never
    /// attempted is <see cref="StepVerdict.Unchecked"/> and carries the absence, because a trace line
    /// reading <em>ok</em> over an act nothing performed is the unearned green in miniature.
    /// </summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = Verb,
        Locator = Locator.Text,
        Resolved = Element.ToString(),
        Pattern = Pattern,
        ReadBack = After.Reading(),
        WaitedMs = WaitedMs,
        Polls = Polls,
        Verdict = Attempted ? StepVerdict.Ok : StepVerdict.Unchecked,
        Detail = Attempted ? null : Needed!.Absence,
    };

    /// <summary>The one line a report shows, with what moved where it moved.</summary>
    public override string ToString()
    {
        // Said before the reading and not after it: "nothing it reports moved" about an act that was
        // never performed is a sentence a reader acts on, and it would be describing the desk.
        if (!Attempted)
            return $"{Verb} {Element} was not attempted: {Needed!.Absence}";

        var moved = Changed
            ? $"{Before.Reading() ?? "nothing"} -> {After.Reading() ?? "nothing"}"
            : "nothing it reports moved";

        return $"{Verb} {Element} via {Pattern}: {moved}";
    }
}
