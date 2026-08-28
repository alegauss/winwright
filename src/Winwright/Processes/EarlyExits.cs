using System.Collections.ObjectModel;

using Winwright.Verdicts;

namespace Winwright.Processes;

/// <summary>
/// Whether every launch a run made was still running when the run came to stop it — the reading
/// <see cref="Survivor" /> is the mirror of, and the one the run rather than a case can take.
/// <para>
/// WW286. WW279 put the exit on the case's own line, and that reaches only a case that owned its
/// process. A fixture declared shareable is launched once and held until the run ends, so
/// <c>Suite.Launch</c> stops it at no case boundary and its exit was recorded on the launch and read
/// by nothing. Meanwhile <see cref="ProcessSummary" /> answers about what <em>outlived</em> the run,
/// and a process that left early outlived nothing — so a run whose shared application died reported
/// <em>no process outlived the run</em>, which is true and is the same unhelpful sentence WW279 was
/// filed against, one level up.
/// </para>
/// <para>
/// Three states rather than two, which is why this is a reading and not a count. Every launch was
/// there when asked; some were not and are named; or nobody has asked yet, which an empty list says
/// in exactly the same words as a clean run — the distinction <see cref="ProcessRegister.Stopped" />
/// exists for, borrowed here for the same reason.
/// </para>
/// <para>
/// A finding and never a failure. A launch that exited is sometimes the defect WW279 is about and
/// sometimes a case closing the application on purpose, and this reading cannot tell those apart —
/// so it says what happened and leaves the colour of the build to the checks that ran.
/// </para>
/// </summary>
/// <param name="Left">Every launch a stop found already gone, in the order the stops happened.</param>
/// <param name="Asked">Whether the roll was taken at all, without which an empty list means nothing.</param>
public sealed record EarlyExits(IReadOnlyList<Departure> Left, bool Asked)
{
    /// <summary>
    /// What this reading is called wherever it is reported. One spelling, for the reason
    /// <see cref="ProcessSummary.Named" /> has one: a preamble and a summary naming it two ways are
    /// two things a reader has to match up by hand.
    /// </summary>
    public const string Named = "every launch this run made was still running when it was asked to stop";

    /// <summary>Whether every launch was still there, which is the claim above.</summary>
    public bool Whole => Asked && Left.Count == 0;

    /// <summary>How many left where only the end of the run could see it, so no case can claim them.</summary>
    public int Unattributable => Left.Count(one => one.Seen == DepartureSeen.WhereTheRunEnded);

    /// <summary>
    /// What was read, said in whichever of the three ways it went. The unattributable ones are
    /// counted apart and say why: an exit seen only at the end of the run happened somewhere inside a
    /// span of cases, and a sentence that put it in one of them would be inventing the half it does
    /// not have.
    /// </summary>
    public string Sentence()
    {
        if (!Asked)
            return "this register was never asked to stop what it launched, so nothing looked.";

        if (Left.Count == 0)
            return "every launch this run made was still running when it was asked to stop.";

        var loose = Unattributable;
        var placed = Left.Count - loose;
        var clauses = new List<string>();
        if (placed > 0)
            clauses.Add($"{placed} where the case that owned it ended");
        if (loose > 0)
        {
            clauses.Add($"{loose} only at the end of the run, so nothing can say which case it went "
                + "during");
        }

        return $"{Left.Count} launch(es) had gone before the run asked: {string.Join(", ", clauses)} — "
            + $"{string.Join(", ", Left.Select(one => $"pid {one.Pid} {one.Executable} {one}"))}.";
    }

    /// <inheritdoc cref="Sentence" />
    public override string ToString() => Sentence();

    /// <summary>The three-state reading, so a caller can carry it rather than branch on it.</summary>
    public Finding AsFinding() => new(Named, Asked ? Whole : null, Sentence());

    /// <summary>
    /// What the register has seen so far. Read after <see cref="ProcessRegister.StopAll" /> for a
    /// reading about the whole run; read before it and <see cref="Asked" /> is false, which is the
    /// answer rather than a shorter version of one.
    /// </summary>
    /// <param name="register">What this run launched.</param>
    public static EarlyExits Of(ProcessRegister register)
    {
        ArgumentNullException.ThrowIfNull(register);

        return new EarlyExits(
            new ReadOnlyCollection<Departure>([.. register.Departures]), register.Stopped);
    }
}
