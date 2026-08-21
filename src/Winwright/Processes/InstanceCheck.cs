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

    private InstanceCheck(string executable, IReadOnlyList<OtherInstance> others, bool allowedOthers)
    {
        Executable = executable;
        Others = others;
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

        foreach (var candidate in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(named)))
            using (candidate)
            {
                if (mine.Contains(candidate.Id) || !IsRunning(candidate, named))
                    continue;

                others.Add(new OtherInstance(candidate.Id, named, TopLevelWindows.OfProcess(candidate.Id)));
            }

        others.Sort((left, right) => left.Pid.CompareTo(right.Pid));
        return new InstanceCheck(named, new ReadOnlyCollection<OtherInstance>(others), allowOthers);
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
        if (Windowed.Count == 0)
            return Resident.Count == 0
                ? "nothing else is running this application."
                : $"{Resident.Count} other instance(s) are resident and showing no window.";

        var listed = string.Join(", ", Windowed.Select(other => $"pid {other.Pid}"));
        return AllowedOthers
            ? $"{Windowed.Count} other windowed instance(s) are open ({listed}) and were allowed by {OverrideName}."
            : $"{Windowed.Count} other windowed instance(s) are open ({listed}).";
    }

    private static bool IsRunning(Process candidate, string executable)
    {
        try
        {
            return string.Equals(candidate.MainModule?.FileName, executable, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception reading) when (reading is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }
}
