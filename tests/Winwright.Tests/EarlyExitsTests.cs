using System.Diagnostics;

using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW286. WW279 put a launch's exit on the case's own line, and that reaches only a case that owned
/// its process. A fixture declared shareable is launched once and held until the run ends, so
/// <c>Suite.Launch</c> stops it at no case boundary — its exit was recorded on the launch and read by
/// nothing, while the survivor sentence went on answering about the opposite thing.
/// <para>
/// These drive real processes rather than constructing the reading, for the reason
/// <c>ProcessRegisterTests</c> does: what is being asserted is which look saw the exit, and a record
/// built by hand would agree with whichever answer this file wrote down.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class EarlyExitsTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-early-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    /// <summary>Something that starts, exits on its own, and needs no console or window.</summary>
    private static ProcessStartInfo Brief()
    {
        var start = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };

        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("exit 7");
        return start;
    }

    /// <summary>Something that stays alive on its own, so the clean arm is about a real process.</summary>
    private static ProcessStartInfo LongRunning()
    {
        var start = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };

        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("ping -n 120 127.0.0.1");
        return start;
    }

    [Fact]
    public void A_register_nobody_asked_says_it_never_looked_rather_than_that_everything_was_running()
    {
        using var register = new ProcessRegister();
        register.Launch(LongRunning());

        // The third state, and the whole reason this is a reading rather than a count: an empty list
        // means both 'nothing left early' and 'nobody has asked yet', which is the distinction
        // WW152 put on the register itself.
        var read = EarlyExits.Of(register);

        Assert.False(read.Asked);
        Assert.False(read.Whole);
        Assert.Null(read.AsFinding().Holds);
        Assert.Contains("never asked", read.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_run_whose_launches_were_all_still_there_says_so_rather_than_saying_nothing()
    {
        using var register = new ProcessRegister();
        register.Launch(LongRunning());
        register.StopAll();

        var read = EarlyExits.Of(register);

        Assert.True(read.Whole);
        Assert.True(read.AsFinding().Holds);
        Assert.Empty(read.Left);
        Assert.Contains("still running when it was asked to stop", read.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void An_exit_only_the_end_of_the_run_saw_is_counted_apart_and_says_why()
    {
        using var register = new ProcessRegister();

        var owned = register.Launch(Brief());
        var lent = register.Launch(Brief());

        // The one a case gives back: a real stop at a real case boundary, waited for so the look is
        // about a process that has gone rather than one still going.
        Gone(owned);
        Assert.Null(register.Stop(owned));

        // And the one nothing stops until the run ends, which is what a lent fixture is.
        Gone(lent);
        register.StopAll();

        var read = EarlyExits.Of(register);
        var said = read.Sentence();

        Assert.Equal(2, read.Left.Count);
        Assert.Equal(1, read.Unattributable);
        Assert.False(read.Whole);
        Assert.False(read.AsFinding().Holds);

        // Counted apart, and the second one says what it cannot say. A sentence that put an exit
        // seen only at the end of the run inside one case would be inventing the half it lacks.
        Assert.Contains("where the case that owned it ended", said, StringComparison.Ordinal);
        Assert.Contains("which case it went during", said, StringComparison.Ordinal);

        // The code the process really exited with, off the process rather than out of this file.
        Assert.Contains("exited with 7", said, StringComparison.Ordinal);
    }

    [Fact]
    public void A_lent_launch_that_died_reaches_the_run_and_no_case_at_all()
    {
        if (!Desk.Read().CanObserve)
            return;

        // The defect itself. Two read-only cases share one resident fixture, so the launch is held
        // until the run ends and `Suite.Launch` stops it at neither of their boundaries.
        using var register = new ProcessRegister();
        var verdict = Suite.Launch(Shared(), Selection.All, register, Project(), sharing: true);

        Assert.Equal(2, verdict.Ran.Count);

        // WW279's clause is silent here, and correctly: neither case can claim an exit that happened
        // somewhere across both of them.
        Assert.All(verdict.Ran, one => Assert.Null(one.Departed));
        Assert.DoesNotContain("exited", string.Join("\n", verdict.Render()), StringComparison.Ordinal);

        // And the run says it, which is what WW286 is. Before this the exit was recorded on the
        // launch and read by nothing anywhere.
        register.StopAll();
        var read = EarlyExits.Of(register);

        Assert.Single(read.Left);
        Assert.Equal(1, read.Unattributable);
        Assert.Contains("which case it went during", read.Sentence(), StringComparison.Ordinal);
        Assert.Contains($"exited with {Fixture.ExitFor("dies")}", read.Sentence(), StringComparison.Ordinal);
    }

    /// <summary>Wait until that launch has gone on its own, so a stop looks at a settled process.</summary>
    private static void Gone(LaunchedProcess launched) => Assert.True(
        launched.WaitForExit(30_000), $"pid {launched.Pid} was supposed to exit on its own and did not");

    /// <summary>A declaration pointing at the fixture, with the waits this file needs.</summary>
    private ProjectDeclaration Project()
    {
        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "timeouts": { "resolve": 300, "act": 1200, "poll": 25, "window": 1500 }
            }
            """);

        return ProjectDeclaration.Load(declaration);
    }

    /// <summary>Two read-only cases over one shareable resident fixture that exits on startup.</summary>
    private static IReadOnlyList<CaseDeclaration> Shared() => ScenarioFile.Read(
        "shared.cases.json",
        """
        {
          "fixtures": [
            {
              "name": "the tray they share",
              "arguments": ["--resident", "--dies"],
              "resident": true,
              "shareable": true
            }
          ],
          "cases": [
            {
              "name": "the first case through the lent launch",
              "catches": "a shared launch that died, whose exit reached no case line and no run summary either",
              "fixture": "the tray they share",
              "onlyReads": true,
              "steps": [
                {
                  "locator": "Button#nothingIsCalledThisOnAnyDesktop",
                  "act": "read",
                  "reads": "name",
                  "answers": true,
                  "named": "the run reached this step at all"
                }
              ]
            },
            {
              "name": "the second case through the same lent launch",
              "catches": "the same, from the case that borrowed the window rather than the one that opened it",
              "fixture": "the tray they share",
              "onlyReads": true,
              "steps": [
                {
                  "locator": "Button#nothingIsCalledThisOnAnyDesktop",
                  "act": "read",
                  "reads": "name",
                  "answers": true,
                  "named": "the run reached this step at all"
                }
              ]
            }
          ]
        }
        """);
}
