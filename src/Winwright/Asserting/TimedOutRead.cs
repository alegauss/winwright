using System.Collections.ObjectModel;

using Winwright.Tracing;
using Winwright.Verdicts;

namespace Winwright.Asserting;

/// <summary>How a read ended, once the two ways of ending with nothing stop being one answer.</summary>
public enum ReadEnded
{
    /// <summary>What was wanted turned up.</summary>
    Arrived,

    /// <summary>
    /// The window was talking the whole time, and everything it said was a working state. A slow
    /// machine, a cold cache, or something that built and cannot be read.
    /// </summary>
    Working,

    /// <summary>Nothing was ever in the tree. The window is not being read at all.</summary>
    Blank,

    /// <summary>It was talking and said something nobody declared as working.</summary>
    Wrong,
}

/// <summary>
/// A read that ended without what it wanted, told apart by what the window was doing.
/// <para>
/// Two timed-out reads look identical to whoever is reading the output and mean opposite things.
/// Working means the window was talking the whole time; blank means nothing was ever in the tree
/// and the window is not being read at all. Collapsing the two cost a defect hunt that started at
/// timing and ended somewhere else entirely — the fault was a missing template part.
/// </para>
/// <para>
/// The observation is <see cref="Expectation"/>'s and is not taken again here. This is the
/// diagnosis over it: which of the endings happened, and what a person should do about it.
/// </para>
/// </summary>
public sealed record TimedOutRead
{
    internal TimedOutRead(Expectation watched, IReadOnlyList<string> working, ReadEnded ended)
    {
        Watched = watched;
        Working = working;
        Ended = ended;
    }

    /// <summary>Everything the read saw, which is where the numbers in the sentence come from.</summary>
    public Expectation Watched { get; }

    /// <summary>The readings this project calls working, as they were declared.</summary>
    public IReadOnlyList<string> Working { get; }

    /// <summary>Which of the four endings happened.</summary>
    public ReadEnded Ended { get; }

    /// <summary>What was there when the wait gave up.</summary>
    public string? LastSeen => Watched.LastRead;

    /// <summary>How many of the polls found the window answering at all.</summary>
    public int PollsThatSaw => Watched.PollsThatSaw;

    /// <summary>Whether anybody declared what working looks like for this project.</summary>
    public bool KnowsWhatWorkingLooksLike => Working.Count > 0;

    /// <summary>
    /// What to do about it, which is the half a timeout message usually leaves out. Each ending
    /// points somewhere different, and pointing everybody at the deadline is how the hunt that
    /// started this went looking at timing for an afternoon.
    /// </summary>
    public string Remedy => Ended switch
    {
        ReadEnded.Arrived => "nothing: it arrived.",
        ReadEnded.Blank => "look at the locator and whether the pane is built at all — not at the deadline, "
            + "which nothing here suggests is the problem.",
        ReadEnded.Working => "raise the deadline only if a slow machine is the answer; a window that works "
            + "forever is usually one whose content built and cannot be read.",
        _ => "read what it said instead: the window was answering, so this is about the value and not "
            + "about the wait.",
    };

    /// <summary>
    /// What happened and what it means. Every ending carries how many polls saw the window,
    /// because that number is what separates the two that read alike.
    /// </summary>
    public string Sentence()
    {
        if (Ended == ReadEnded.Arrived)
            return Watched.Sentence();

        var seen = $"{PollsThatSaw} of {Watched.Polls} polls saw it over {Watched.WaitedMs}ms";

        var said = Ended switch
        {
            ReadEnded.Blank => $"{Watched.Name} was blank: nothing was ever in the tree, and {seen}.",
            ReadEnded.Working => $"{Watched.Name} was working for the whole wait, last reading "
                + $"'{LastSeen}', and {seen}.",
            _ => $"{Watched.Name} read '{LastSeen}', which nothing declares as working, and {seen}.",
        };

        // The hole is printed rather than hidden. Without declared working text every busy window
        // lands in Wrong, and a reader who is not told that reads a confident diagnosis.
        var undeclared = KnowsWhatWorkingLooksLike
            ? ""
            : " No working text is declared for this project, so a window that was busy cannot be told"
                + " from one that answered wrongly.";

        return $"{said} What to do: {Remedy}{undeclared}";
    }

    /// <summary>The result a verdict counts, carrying that sentence as its detail.</summary>
    public AssertionResult AsAssertion() => Ended == ReadEnded.Arrived
        ? AssertionResult.Pass(Watched.Name, Sentence())
        : AssertionResult.Fail(Watched.Name, Sentence());

    /// <summary>
    /// The step a trace records. WW163: this is the diagnosis of a read that ended without what it
    /// wanted, which is the one step a reader most wants the record to have kept.
    /// </summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = "read",
        Locator = Watched.Name,
        ReadBack = Watched.LastRead,
        WaitedMs = Watched.WaitedMs,
        Polls = Watched.Polls,
        Verdict = Ended == ReadEnded.Arrived ? StepVerdict.Ok : StepVerdict.Failed,
        Detail = Ended == ReadEnded.Arrived ? null : Sentence(),
    };
}

/// <summary>Diagnosing a read that ended without what it wanted.</summary>
public static class Reads
{
    /// <summary>
    /// Say how a read ended, given what this project calls working.
    /// </summary>
    /// <param name="expectation">What the read saw. The observation is not taken again.</param>
    /// <param name="working">
    /// The readings that mean the window is busy. Empty is allowed and is reported in the
    /// sentence rather than refused: a project may genuinely have none, and a run that quietly
    /// treated every busy window as a wrong one would be the same collapse in a new place.
    /// </param>
    public static TimedOutRead Diagnose(Expectation expectation, params string[] working)
    {
        ArgumentNullException.ThrowIfNull(expectation);
        ArgumentNullException.ThrowIfNull(working);

        var declared = new ReadOnlyCollection<string>(
            working.Where(text => !string.IsNullOrWhiteSpace(text)).ToList());

        return new TimedOutRead(expectation, declared, Ending(expectation, declared));
    }

    /// <summary>
    /// The same, with the working readings derived from the project's own strings rather than
    /// typed — which is what keeps this from being one more hardcoded list to fall behind.
    /// </summary>
    public static TimedOutRead Diagnose(Expectation expectation, DerivedSet working)
    {
        ArgumentNullException.ThrowIfNull(working);
        return Diagnose(expectation, [.. working.Expected]);
    }

    private static ReadEnded Ending(Expectation expectation, IReadOnlyList<string> working)
    {
        if (expectation.Held)
            return ReadEnded.Arrived;

        if (!expectation.EverSaw)
            return ReadEnded.Blank;

        // Every reading, and not merely the last one. A window that said "Computing" and then
        // "Could not load" was not working the whole time: it stopped and told you something,
        // and calling that Working would send the reader back to the deadline.
        var everything = expectation.Readings
            .Where(reading => reading.Read is not null)
            .All(reading => working.Contains(reading.Read!, StringComparer.Ordinal));

        return everything ? ReadEnded.Working : ReadEnded.Wrong;
    }
}
