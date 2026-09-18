using System.Diagnostics;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// Where the built fixture is, and how to start it.
/// <para>
/// The suite references the fixture project without its assembly and copies nothing from it, so the
/// only handle it has on it is a path — an application under test is launched from its own output,
/// not read from beside the harness. Copying it once brought the apphost without its assembly and
/// every launch died at CLR startup.
/// </para>
/// <para>
/// WW145 made this the third file wanting the path, which is the signal WW144 named: two copies of
/// a small parser is a helper waiting to be written. It lives here once.
/// </para>
/// </summary>
internal static class Fixture
{
    /// <summary>The built fixture, in whatever configuration the suite itself was built in.</summary>
    public static string Executable()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);
        var framework = here.Name;
        var configuration = here.Parent!.Name;

        var path = Checkout.At(
            "src", "Winwright.Fixture", "bin", configuration, framework, "Winwright.Fixture.exe");

        Assert.True(File.Exists(path), $"the fixture was not built: {path}");
        return path;
    }

    /// <summary>
    /// The strings the fixture ships, beside the executable. Read as a directory rather than
    /// through the fixture's own type, for the reason above: the suite cannot see its types.
    /// </summary>
    public static string StringsDirectory()
    {
        var strings = Path.Combine(Path.GetDirectoryName(Executable())!, "strings");
        Assert.True(Directory.Exists(strings), $"the fixture shipped no strings: {strings}");
        return strings;
    }

    /// <summary>
    /// One of those files, by its language tag. WW468: a case comparing the two wells needs the same
    /// file the application reads, and finding it by the tag is what keeps the comparison honest — a
    /// path typed here would be a third transcription agreeing with neither.
    /// </summary>
    /// <param name="tag">The language tag, as the fixture spells its file names.</param>
    public static string StringsFor(string tag)
    {
        var file = Path.Combine(StringsDirectory(), $"strings.{tag}.json");
        Assert.True(File.Exists(file), $"the fixture ships no strings for {tag}: {file}");
        return file;
    }

    /// <summary>The fixture's start info, with whatever flags a case wants.</summary>
    public static ProcessStartInfo Started(params string[] flags)
    {
        var start = new ProcessStartInfo(Executable());
        foreach (var flag in flags)
            start.ArgumentList.Add(flag);

        return start;
    }

    /// <summary>
    /// The window a launched fixture drew, waited for on the deadline this suite declares.
    /// <para>
    /// WW448. Twelve cases wrote this out: the same look at the same process, a budget of 20000 and
    /// a poll of 25, followed by the same assertion. WW446 counted them, because the catalogue it
    /// widened had never seen the spelling they use — and counting them is what showed they were one
    /// wait, written where it was needed each time.
    /// </para>
    /// <para>
    /// What the copies also did was bypass the declaration. <c>Waits</c> has named this since WW143
    /// — <c>draw</c>, ten seconds, argued as the cold start of a fixture on a runner that has never
    /// run it — and twelve cases typed twice that instead, which is the number nobody could tune
    /// because nobody could find it. The wait is on the declared deadline now and the number is gone
    /// from every case.
    /// </para>
    /// <para>
    /// A launch is not folded in with it. Two callers launch through doors of their own — the suite's
    /// own launch, and the typing rig's, which is a different assembly and keeps its copy — so what
    /// is shared is the wait and not the starting.
    /// </para>
    /// </summary>
    /// <param name="pid">The process that was launched.</param>
    public static Winwright.Windowing.TopLevelWindow Drew(int pid) =>
        Waits.Until("draw", $"the fixture ({pid}) drew no window", () => Winwright.Windowing.TopLevelWindows.Largest(pid));

    /// <summary>The catalogue, as the built fixture prints it.</summary>
    public static string Catalogue()
    {
        var start = Started("--flags");
        start.RedirectStandardOutput = true;
        start.UseShellExecute = false;

        using var running = Process.Start(start)!;
        var said = running.StandardOutput.ReadToEnd();
        Assert.True(running.WaitForExit(30_000), "the catalogue never finished printing");
        Assert.Equal(0, running.ExitCode);

        return said;
    }

    /// <summary>
    /// The word <c>--announces</c> writes in front of a marked row's sentence.
    /// <para>
    /// WW83. Spelled here as well as in <c>AnnouncesPane</c>, which is a duplication and the only
    /// shape available: the suite references the fixture without its assembly on purpose, so a
    /// constant over there is one this file cannot name. It is the safe direction to drift in — the
    /// two disagreeing makes the claim about the front of a sentence fail, which is the claim under
    /// test, rather than passing against a state nobody is in.
    /// </para>
    /// </summary>
    public const string AnnouncedChecked = "Checked";

    /// <summary>And the word it writes in front of an unmarked one. Duplicated for the same reason.</summary>
    public const string AnnouncedUnchecked = "Not checked";

    /// <summary>
    /// What it appends to the name of the row being followed. WW85, duplicated for the same reason
    /// as the two above and safe in the same direction: the two disagreeing fails the claim about
    /// the end of a name, which is the claim under test.
    /// </summary>
    public const string AnnouncedAppended = "· active now";

    /// <summary>
    /// What the shell calls the icon a <c>--tray</c> launch puts up. WW451.
    /// <para>
    /// The tip carries the launched process, which is WW126's rule and matters more across a process
    /// boundary than it did inside one: a run that was killed leaves its icon registered with the
    /// shell, nothing can delete somebody else's, and a ghost found by tip would be driven as this
    /// launch's own. So a case addresses the icon by the process it started rather than by a name.
    /// </para>
    /// <para>
    /// Spelled here as well as in <c>Trayed.TipFor</c>, for the reason <see cref="AnnouncedChecked" />
    /// gives: this suite references the fixture without its assembly on purpose, so a member over
    /// there is one this file cannot call. It is the safe direction to drift in — the two disagreeing
    /// makes the case fail to find an icon that is standing, which is a red about the thing under
    /// test rather than a green against a tray nobody launched.
    /// </para>
    /// </summary>
    /// <param name="pid">The launched fixture.</param>
    public static string TrayTip(int pid) => $"winwright tray #{pid}";

    /// <summary>
    /// What a run asking for that shape exits with, read off the catalogue.
    /// <para>
    /// WW161. The suite used to carry the number as a private constant copied out of the fixture,
    /// which is a second transcription of the same fact and the exact thing the catalogue was built
    /// to stop. A case asserting 3 now reads it the way it already reads the flag names: out of the
    /// article, so a fixture that changed its code fails the case rather than agreeing with a copy.
    /// </para>
    /// </summary>
    /// <param name="flag">The shape, without its dashes.</param>
    public static int ExitFor(string flag)
    {
        var row = Assert.Single(
            Catalogue().Split('\n').Select(one => one.Trim()),
            one => one.StartsWith($"--{flag} ", StringComparison.Ordinal)
                || one.StartsWith($"--{flag}=", StringComparison.Ordinal));

        var says = System.Text.RegularExpressions.Regex.Match(row, @"\[exits (\d+)\]");
        Assert.True(says.Success, $"the catalogue's row for --{flag} says nothing about what it exits with: {row}");

        return int.Parse(says.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }
}
