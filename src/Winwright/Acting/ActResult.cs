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
        ElementFacts? element,
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

    /// <summary>
    /// What the element was, read at the moment the act ran — and null where the locator matched
    /// nothing at all.
    /// <para>
    /// WW321. It used to assert one, and that assertion was the whole of a run that came back
    /// <c>Broken</c> over a menu that did not open: the case before it had already failed, so the
    /// locator this act named matched nothing, and building the trace line threw out of the engine's
    /// own frames. Broken outranks Failed and means the harness threw, which is the one verdict that
    /// sends a reader to the wrong repository.
    /// </para>
    /// <para>
    /// An act against nothing is a real thing to report and not an error: what was needed of the
    /// machine, what the locator was and that nothing answered to it are all still here. Only the
    /// element is missing, because there was none.
    /// </para>
    /// </summary>
    public ElementFacts? Element { get; }

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
        Resolved = Element?.ToString(),
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
            return $"{Verb} {Named} was not attempted: {Needed!.Absence}";

        var moved = Changed
            ? $"{Before.Reading() ?? "nothing"} -> {After.Reading() ?? "nothing"}"
            : "nothing it reports moved";

        return $"{Verb} {Named} via {Pattern}: {moved}";
    }

    /// <summary>
    /// The element as a sentence names it, and the word for having matched nothing. Never the empty
    /// string an interpolated null would leave: "click  was not attempted" reads as a formatting
    /// slip, and what happened is that the locator found no element to click.
    /// </summary>
    private string Named => Element?.ToString() ?? $"nothing matching {Locator.Text}";
}
