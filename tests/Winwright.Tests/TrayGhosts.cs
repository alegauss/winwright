using System.Diagnostics;
using System.Text.RegularExpressions;

using Winwright.Acting;
using Winwright.Verdicts;

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
    /// wait does it. Where the shell will not open it — a Start menu covering the taskbar, no
    /// chevron in the tree — the reading says so and never that there were none.
    /// </para>
    /// </summary>
    internal static TrayCensus Showing()
    {
        var names = new List<string>(NotificationArea.Showing().Select(one => one.Name));

        var opened = NotificationArea.OpenOverflow();
        if (!opened.Held)
        {
            // WW181. Half a desk read, said as half. Answering the taskbar's ghosts here as though
            // they were all of them is a green covering what never ran, which is the one thing this
            // project refuses — and it shipped inside the reading that was meant to stop it.
            return new TrayCensus(Among(names, Running), everywhere: false, opened.Because ?? opened.ToString());
        }

        names.AddRange(NotificationArea.Hidden().Select(one => one.Name));
        if (!opened.Already)
            NotificationArea.CloseOverflow();

        return new TrayCensus(Among(names, Running), everywhere: true, "");
    }
}

/// <summary>
/// What a census of the notification area found, and whether it got to look everywhere.
/// <para>
/// WW181. The third state the <see cref="Winwright.Verdicts.Finding" /> shape has and the first
/// spelling of this reading did not: seen and clean, seen and holding, and not read. A reading with
/// two states can only report the third as one of the other two, and the one it picked was clean.
/// </para>
/// </summary>
internal sealed record TrayCensus
{
    internal TrayCensus(IReadOnlyList<string> ghosts, bool everywhere, string because)
    {
        Ghosts = ghosts;
        Everywhere = everywhere;
        Because = because;
    }

    /// <summary>The icons an ended run left, among the places this reading got to look.</summary>
    internal IReadOnlyList<string> Ghosts { get; }

    /// <summary>
    /// Whether the overflow was read as well as the taskbar. False makes <see cref="Ghosts" /> a
    /// floor and never a count: what is hiding in a flyout nobody opened is not nothing.
    /// </summary>
    internal bool Everywhere { get; }

    /// <summary>Why the overflow was not read, where it was not. Empty where it was.</summary>
    internal string Because { get; }

    /// <summary>Whether this reading is entitled to say the desk is clean.</summary>
    internal bool Clean => Everywhere && Ghosts.Count == 0;

    /// <summary>
    /// The reading a person gets, in three states. A clean desk says so rather than saying nothing
    /// and leaving silence to be read as either answer — which is the shape
    /// <see cref="Winwright.Processes.ProcessSummary" /> already uses for the processes a run had to
    /// stop, and this is the same fact one surface over.
    /// </summary>
    internal string Sentence()
    {
        var held = Ghosts.Count == 0
            ? ""
            : $"the notification area still holds {Ghosts.Count} icon(s) this suite added in a run that has "
                + $"ended, which no process here can withdraw: {string.Join(", ", Ghosts)}.";

        if (Everywhere)
            return held.Length == 0 ? "nothing this suite added is still in the notification area." : held;

        var unread = $"the overflow was not read, so what is hiding there was not looked at: {Because}";
        return held.Length == 0 ? $"{unread}." : $"{held} And {unread}.";
    }

    /// <summary>
    /// This reading as the engine's own three-state one.
    /// <para>
    /// WW182. The first spelling of this was a sentence and a list, and a list has no way to say
    /// "I did not look" — so the third state had nowhere to live and the sentence rounded it down to
    /// the second, which is how WW181 shipped a clean desk it had never read. <see cref="Finding" />
    /// carries <c>bool? Holds</c> and was three tasks old when that happened, and its own comment
    /// gives WW151's reason: two states could only ever report the third as one of the other two.
    /// </para>
    /// <para>
    /// Answered rather than replaced-with. The census keeps the fields a case reads — which ghosts,
    /// and whether the overflow was reached — and this is the shape a report joins, so a run could
    /// carry what it left in the shell beside what it left in the store.
    /// </para>
    /// </summary>
    internal Finding AsFinding() => new(TrayGhosts.Named, Everywhere ? Ghosts.Count == 0 : null, Sentence());

    /// <summary>
    /// The verdict a case counts. A shell that would not open the flyout never got asked the
    /// question, so it is a hole under the name the search one file over already uses for it.
    /// </summary>
    /// <param name="named">What the assertion claims.</param>
    internal AssertionResult AsAssertion(string named)
    {
        if (!Everywhere)
            return AssertionResult.Unchecked(named, Precondition.Absent(TraySearch.PreconditionName, Sentence()));

        return Ghosts.Count == 0 ? AssertionResult.Pass(named, Sentence()) : AssertionResult.Fail(named, Sentence());
    }
}
