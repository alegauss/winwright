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

        var repository = here;
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "Winwright.slnx")))
            repository = repository.Parent;

        Assert.NotNull(repository);
        var path = Path.Combine(
            repository.FullName, "src", "Winwright.Fixture", "bin", configuration, framework, "Winwright.Fixture.exe");

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

    /// <summary>The fixture's start info, with whatever flags a case wants.</summary>
    public static ProcessStartInfo Started(params string[] flags)
    {
        var start = new ProcessStartInfo(Executable());
        foreach (var flag in flags)
            start.ArgumentList.Add(flag);

        return start;
    }

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
