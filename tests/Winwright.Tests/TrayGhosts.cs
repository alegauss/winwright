using System.Diagnostics;
using System.Text.RegularExpressions;

using Winwright.Acting;

namespace Winwright.Tests;

/// <summary>
/// What this suite left in the notification area, from a run that is no longer running.
/// <para>
/// WW173. Measured across three guest runs of one tree. The first died with the Start menu holding
/// the foreground. The second came back with four failures, all of them the tray fixture waiting
/// five seconds and reporting that the shell took the icon and never placed it. No process outlived
/// any run — the guest's process list was checked — so what survived was state inside the shell, and
/// the run that created it was the one that died. Restarting Explorer took the tree to green.
/// </para>
/// <para>
/// WW126 already made a ghost harmless: the tip carries the pid that added it, so a ghost is never
/// read as this run's own icon. What it did not do is say one is there. Four opaque reds and a
/// shell restart is what that costs, and this is the sentence they needed.
/// </para>
/// <para>
/// A finding and never a failure. The shell keeping an icon whose owner is gone is a fact about the
/// desk, and nothing in this repository can withdraw somebody else's registration — so naming it is
/// the whole of what is available, which is exactly what makes naming it worth doing.
/// </para>
/// </summary>
internal static class TrayGhosts
{
    /// <summary>What this reading is called wherever it is reported.</summary>
    internal const string Named = "nothing this suite added is still in the notification area";

    /// <summary>
    /// The mark WW126 puts on every icon this suite adds. Matched rather than reconstructed: a
    /// second spelling of the tip format is one that goes stale the day the first one changes.
    /// </summary>
    private static readonly Regex Marked = new(@"#(\d+)\s*$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    /// <summary>
    /// Which of these names belong to a run that has ended. Pure, and separated from the reading
    /// for one reason: provoking a real ghost means killing a run mid-flight, and a rule that can
    /// only be checked that way is a rule nothing checks.
    /// </summary>
    /// <param name="names">The tips the notification area is showing.</param>
    /// <param name="alive">Whether a pid is still running.</param>
    internal static IReadOnlyList<string> Among(IEnumerable<string> names, Func<int, bool> alive)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(alive);

        var ghosts = new List<string>();
        foreach (var name in names)
        {
            var first = name.Split('\n')[0].Trim();
            if (!first.StartsWith("winwright ", StringComparison.Ordinal))
                continue;

            var mark = Marked.Match(first);

            // An unmarked winwright icon is not judged either way. It is not this suite's to
            // classify, and calling it a ghost would report a leftover nobody can act on.
            if (!mark.Success || !int.TryParse(mark.Groups[1].Value, out var pid))
                continue;

            if (!alive(pid))
                ghosts.Add(first);
        }

        return ghosts;
    }

    /// <summary>Whether a pid is a process that is still running.</summary>
    internal static bool Running(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (Exception gone) when (gone is ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// What the notification area is holding now that an earlier run left, read off the taskbar and
    /// off the overflow where the shell will open it.
    /// <para>
    /// The flyout is opened and shut again rather than left standing, the way the fixture's own
    /// wait does it. Where the shell will not open it, the ghosts hiding there are simply not seen —
    /// this reading says what it saw and never that there were none.
    /// </para>
    /// </summary>
    internal static IReadOnlyList<string> Showing()
    {
        var names = new List<string>(NotificationArea.Showing().Select(one => one.Name));

        var opened = NotificationArea.OpenOverflow();
        if (opened.Held)
        {
            names.AddRange(NotificationArea.Hidden().Select(one => one.Name));
            if (!opened.Already)
                NotificationArea.CloseOverflow();
        }

        return Among(names, Running);
    }

    /// <summary>
    /// The sentence, said either way. A clean desk says so rather than saying nothing and leaving
    /// silence to be read as either answer — which is the shape <see cref="Winwright.Processes.ProcessSummary" />
    /// already uses for the processes a run had to stop, and this is the same fact one surface over.
    /// </summary>
    /// <param name="ghosts">What <see cref="Showing" /> found.</param>
    internal static string Sentence(IReadOnlyList<string> ghosts)
    {
        ArgumentNullException.ThrowIfNull(ghosts);

        if (ghosts.Count == 0)
            return "nothing this suite added is still in the notification area.";

        return $"the notification area still holds {ghosts.Count} icon(s) this suite added in a run that has "
            + $"ended, which no process here can withdraw: {string.Join(", ", ghosts)}.";
    }
}
