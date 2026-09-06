using System.Diagnostics;

using Winwright.Typing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW393. Three tasks were spent on the arms and none of them ran one.
/// <para>
/// WW354 held the names to <c>run-typing.cmd</c> in both directions, WW367 made each arm carry the
/// code it runs, and WW380 made that code nameable so a case could say which runner an arm reaches.
/// Every one is a claim about a shape — a list, a constructor parameter, a delegate's declaring
/// type — and a runner that throws on its first statement satisfies all three.
/// </para>
/// <para>
/// The reason nothing ran one is right and covers less than it was taken to. The tool takes the desk
/// for minutes and a guest run should not pay for a question asked once — but what costs minutes is
/// the measurement, and every runner takes its round count as an argument. At one round the whole
/// set is seconds of desk and every line of every runner is reached.
/// </para>
/// <para>
/// A case here and not a <c>--smoke</c> somebody types, which is what WW393 had to decide. The two
/// differ in when a broken runner is found: on the run that broke it, or when the next person
/// measures. WW368 spent thirty minutes of guest desk learning that <c>transfer</c> ran at all,
/// which is the price of the second answer paid once.
/// </para>
/// <para>
/// One round is a smoke test and says so. What it asserts is that the runner ran and wrote its own
/// reading — not what the reading said, which at one round is a number about nothing. A rate needs
/// the count the .cmd's own prose argues for, and this deliberately never asks for one.
/// </para>
/// <para>
/// Serial, for WW125's rule and its own sake: this launches a process that takes the foreground and
/// types into it, which is the thing that rule is about.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class TypingArmRunTests
{
    /// <summary>How long one arm gets at a single round, before this stops waiting on it.</summary>
    private const int WithinMs = 120_000;

    [Fact]
    public void Every_arm_runs_a_round_and_writes_its_own_reading()
    {
        // Every arm in one case and every failure reported together, for WW392's reason: five cases
        // would find the first broken runner and cost a guest run apiece to find the next.
        var broke = new List<string>();

        foreach (var arm in Arms.All)
        {
            var (code, said) = Ran("1", arm.Name);

            if (code != 0)
            {
                broke.Add($"--{arm.Name} exited {code}: {Tail(said)}");
                continue;
            }

            // Its own words and not merely a clean exit. Every runner opens with a sentence naming
            // the task it was built for, so an arm that printed nothing ran nothing — and a tool
            // that counts and never fails is one an exit code alone cannot speak for.
            if (!said.Contains(arm.Task, StringComparison.Ordinal))
                broke.Add($"--{arm.Name} exited 0 and never named {arm.Task}: {Tail(said)}");
        }

        // And the bare run, which is the one experiment that is not in the list: WW354 kept it out
        // deliberately, so it is the arm a catalogue check can never reach and the one this has to
        // name for itself.
        var (bare, bareSaid) = Ran("1");
        if (bare != 0)
            broke.Add($"a bare run exited {bare}: {Tail(bareSaid)}");

        Assert.True(
            broke.Count == 0,
            $"{broke.Count} of {Arms.All.Count + 1} runner(s) did not survive a single round:"
                + $"{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", broke)}");
    }

    /// <summary>
    /// Run the typing tool and answer what it exited with and what it said. WW393.
    /// <para>
    /// The dll through <c>dotnet</c>, which is what <c>run-typing.cmd</c> types: the tool finds the
    /// fixture from its own directory, so running it any other way would be measuring a launch this
    /// project does not have.
    /// </para>
    /// </summary>
    /// <param name="arguments">The rounds, and the arm where one is named.</param>
    private static (int Code, string Said) Ran(params string[] arguments)
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);
        var tool = Checkout.At(
            "tools", "Winwright.Typing", "bin", here.Parent!.Name, here.Name, "Winwright.Typing.dll");

        Assert.True(File.Exists(tool), $"the typing tool was not built: {tool}");

        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,

            // No console of its own, for the reason every launch in this suite suppresses one: a
            // window this process opens is a window that can take the desk from the fixture the
            // tool is about to type into.
            CreateNoWindow = true,
        };

        start.ArgumentList.Add(tool);
        foreach (var one in arguments)
            start.ArgumentList.Add(one);

        using var running = Process.Start(start)!;
        var said = running.StandardOutput.ReadToEnd();
        var wrong = running.StandardError.ReadToEnd();

        Assert.True(running.WaitForExit(WithinMs), $"the typing tool did not finish in {WithinMs}ms");

        return (running.ExitCode, said + wrong);
    }

    /// <summary>The end of what it wrote, which is where a runner that threw says so.</summary>
    /// <param name="said">Everything it printed.</param>
    private static string Tail(string said)
    {
        var lines = said.Split('\n').Select(one => one.TrimEnd('\r')).Where(one => one.Trim().Length > 0).ToList();
        return lines.Count == 0 ? "<it printed nothing>" : string.Join(" | ", lines.TakeLast(4));
    }
}
