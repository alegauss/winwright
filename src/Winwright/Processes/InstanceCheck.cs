using System.Collections.ObjectModel;
using System.Diagnostics;

using Winwright.Windowing;

namespace Winwright.Processes;

/// <summary>One other process running the same executable, and the windows it is showing.</summary>
/// <param name="Pid">Its process id.</param>
/// <param name="Executable">The binary it is running, which is the one under test.</param>
/// <param name="Windows">Every top-level window it is showing, largest first. Empty for a resident tray.</param>
public sealed record OtherInstance(int Pid, string Executable, IReadOnlyList<TopLevelWindow> Windows)
{
    /// <summary>Whether it is showing anything, which is the only kind that gets in the way.</summary>
    public bool Windowed => Windows.Count > 0;

    /// <summary>The one line a refusal or a summary shows.</summary>
    public override string ToString() =>
        Windowed ? $"pid {Pid} ({Windows[0]})" : $"pid {Pid} (no window)";
}

/// <summary>
/// A process carrying the right name that would not say which binary it is running.
/// <para>
/// WW180. Skipping one is the right policy and the type's own comment gives the reason: refusing on
/// everything unreadable is refusing on an elevated shell somebody left open. What was wrong is that
/// the skipping was silent — a count of one where the machine had two, with nothing saying a second
/// candidate had been passed over.
/// </para>
/// <para>
/// Measured as a race: a process created a moment earlier has not mapped its image yet, so it
/// answers nothing to the question and drops out of the count. Twice in eight guest runs.
/// </para>
/// </summary>
/// <param name="Pid">The process that could not be identified.</param>
/// <param name="Because">Why it could not be, in the words the reading was refused with.</param>
public sealed record Unidentified(int Pid, string Because)
{
    /// <summary>The one line a summary names it by.</summary>
    public override string ToString() => $"pid {Pid} ({Because})";
}

/// <summary>
/// Whether anything else is already showing the application under test.
/// <para>
/// Only a windowed instance counts, and that is the whole judgement here: a resident tray showing
/// nothing runs on every developer machine this tool was written on, and a check that fired on it
/// would make every routine capture take an override to work — which is an override everybody
/// passes always, and therefore a check nobody has.
/// </para>
/// </summary>
public sealed class InstanceCheck
{
    /// <summary>What a caller passes to run anyway, named in the refusal and in the summary.</summary>
    public const string OverrideName = "allowOtherInstances";

    private InstanceCheck(
        string executable,
        IReadOnlyList<OtherInstance> others,
        IReadOnlyList<Unidentified> unreadable,
        bool allowedOthers)
    {
        Executable = executable;
        Others = others;
        Unreadable = unreadable;
        AllowedOthers = allowedOthers;
        Windowed = new ReadOnlyCollection<OtherInstance>(others.Where(other => other.Windowed).ToList());
        Resident = new ReadOnlyCollection<OtherInstance>(others.Where(other => !other.Windowed).ToList());
    }

    /// <summary>The application under test, resolved.</summary>
    public string Executable { get; }

    /// <summary>Every other process running it, windowed or not.</summary>
    public IReadOnlyList<OtherInstance> Others { get; }

    /// <summary>The ones showing a window, which are the ones in the way.</summary>
    public IReadOnlyList<OtherInstance> Windowed { get; }

    /// <summary>The ones showing nothing — the ordinary case, and never a reason to stop.</summary>
    public IReadOnlyList<OtherInstance> Resident { get; }

    /// <summary>
    /// Candidates carrying the right name that would not say what they are running.
    /// <para>
    /// WW180. Not counted, deliberately, and not silent either: this is the difference between a
    /// reading that says "nothing else is running it" and one that says "nothing else that would
    /// answer". A caller who needs certainty has <see cref="Certain" />; one who does not is no
    /// worse off than before.
    /// </para>
    /// </summary>
    public IReadOnlyList<Unidentified> Unreadable { get; }

    /// <summary>Whether every candidate answered, so a count of none means none.</summary>
    public bool Certain => Unreadable.Count == 0;

    /// <summary>Whether the caller said to run anyway.</summary>
    public bool AllowedOthers { get; }

    /// <summary>Whether this run must stop before it drives the wrong window.</summary>
    public bool Refuses => Windowed.Count > 0 && !AllowedOthers;

    /// <summary>
    /// Look for other processes running <paramref name="executable"/> and read what each is
    /// showing. A process this run owns is named in <paramref name="ours"/> and is never another
    /// instance; a process that will not say what it is running is skipped rather than counted,
    /// because refusing on everything unreadable is refusing on an elevated shell somebody left open.
    /// </summary>
    public static InstanceCheck Of(string executable, IEnumerable<int>? ours = null, bool allowOthers = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);

        var named = Path.GetFullPath(executable);
        var mine = new HashSet<int>(ours ?? []);
        var others = new List<OtherInstance>();

        var unreadable = new List<Unidentified>();

        foreach (var candidate in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(named)))
            using (candidate)
            {
                if (mine.Contains(candidate.Id))
                    continue;

                var running = Running(candidate);
                if (running.Because.Length > 0)
                {
                    // WW180: passed over as before, and now said. A candidate carrying the right
                    // name that will not answer is neither an instance nor nothing.
                    unreadable.Add(new Unidentified(candidate.Id, running.Because));
                    continue;
                }

                if (!string.Equals(running.Binary, named, StringComparison.OrdinalIgnoreCase))
                    continue;

                others.Add(new OtherInstance(candidate.Id, named, TopLevelWindows.OfProcess(candidate.Id)));
            }

        others.Sort((left, right) => left.Pid.CompareTo(right.Pid));
        unreadable.Sort((left, right) => left.Pid.CompareTo(right.Pid));

        return new InstanceCheck(
            named,
            new ReadOnlyCollection<OtherInstance>(others),
            new ReadOnlyCollection<Unidentified>(unreadable),
            allowOthers);
    }

    /// <summary>Stop here where anything else is showing this application.</summary>
    /// <exception cref="AnotherInstanceException">Where one is, and the override was not passed.</exception>
    public void RequireSole()
    {
        if (Refuses)
            throw new AnotherInstanceException(Executable, Windowed);
    }

    /// <summary>
    /// What was found, said either way — and the override named where it was used, because an
    /// override that does not appear in the output is one nobody remembers passing.
    /// </summary>
    public string Sentence()
    {
        // WW180. Appended rather than woven in, and appended to every arm: the one that most needed
        // it is "nothing else is running this application", which is the sentence a passed-over
        // candidate turns into a claim nobody checked.
        var passed = Unreadable.Count == 0
            ? ""
            : $" {Unreadable.Count} candidate(s) carrying the name would not say what they are running "
                + $"and were passed over: {string.Join(", ", Unreadable)}.";

        if (Windowed.Count == 0)
        {
            var none = Resident.Count == 0
                ? "nothing else is running this application."
                : $"{Resident.Count} other instance(s) are resident and showing no window.";

            return none + passed;
        }

        var listed = string.Join(", ", Windowed.Select(other => $"pid {other.Pid}"));
        var open = AllowedOthers
            ? $"{Windowed.Count} other windowed instance(s) are open ({listed}) and were allowed by {OverrideName}."
            : $"{Windowed.Count} other windowed instance(s) are open ({listed}).";

        return open + passed;
    }

    /// <summary>
    /// Which binary a candidate is running, or why it would not say. Two answers rather than one,
    /// because "it is something else" and "it would not tell me" are two facts and a bool could
    /// only ever report them the same way.
    /// </summary>
    private static (string Binary, string Because) Running(Process candidate)
    {
        try
        {
            return (candidate.MainModule?.FileName ?? "", "");
        }
        catch (Exception reading) when (reading is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return ("", reading.Message);
        }
    }
}
