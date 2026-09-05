using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW379. The two instants a harness has to tell apart, against a control that actually has two.
/// <para>
/// WW366 found that an act reads once — the moment it returns — while the expectation beside it
/// polls after that, and put what the verdict settled on onto the act's own trace line. What it
/// could not do is drive the case: every control this fixture had answered the instant it was
/// asked, so the check that landed was pinned through a reading that differs by <em>projection</em>
/// rather than by timing.
/// </para>
/// <para>
/// The box this drives has both. It reads back what was written to it and then, a declared moment
/// later, what the application made of it — a commit that echoes back differently, which is what a
/// form does. So the act's reading and the verdict's are two different values about one control,
/// which is the whole of what WW366 is about and the state nothing here could produce.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class SettlesLateTests : IDisposable
{
    /// <summary>
    /// How long the box takes to have its own say. Long enough that the act cannot read the settled
    /// value by luck, short enough that the expectation's own budget is never the thing under test.
    /// </summary>
    private const int LateMs = 400;

    /// <summary>
    /// What the box is called. Spelled here rather than referenced: the fixture is an application
    /// under test and this suite carries no reference to its assembly, which is the rule every other
    /// locator in these cases already follows — and what holds the two spellings together is a case
    /// that drives the real window and finds it.
    /// </summary>
    private const string BoxId = "settlesBox";

    private readonly Settling settling = Attachable.Settling();
    private readonly string root = Directory.CreateTempSubdirectory("winwright-settles-").FullName;
    private readonly AutomationElement? window;

    public SettlesLateTests()
    {
        if (!Desk.Read().CanObserve)
            return;

        var launched = settling.Register.Launch(Fixture.Started($"--settles={LateMs}"));
        var drawn = Attempt.UntilTrue(() => TopLevelWindows.Largest(launched.Pid) is not null, 20000, 25);

        Assert.True(drawn.Happened, $"the fixture drew no window in {drawn.WaitedMs}ms");
        window = AutomationElement.FromHandle(TopLevelWindows.Largest(launched.Pid)!.Handle);
    }

    public void Dispose()
    {
        settling.Dispose();
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void The_act_reads_one_value_and_the_verdict_turns_on_the_one_that_arrived_after_it()
    {
        if (window is null)
            return;

        // Written lower case and read back upper: the act sees what it wrote, and the application
        // has its say while the expectation is still polling.
        var declared = CaseDeclaration.Of(
            "the box settles on what the application made of it",
            StepDeclaration.Of(
                $"Edit#{BoxId}", "set value", "beta", expected: "BETA", reads: "value"));

        var run = CaseRun.Of(declared, window, Declared());

        Assert.True(
            run.Verdict.Outcome == Winwright.Verdicts.RunOutcome.Passed,
            string.Join(
                Environment.NewLine,
                run.Verdict.Results.Select(one => $"  result    {one}")
                    .Prepend($"  outcome   {run.Verdict.Outcome}")));

        Assert.Equal(2, run.Trace.Count);

        // The act's own reading, taken the moment it returned. This is the value WW366 is about:
        // true, and not what the verdict used.
        Assert.Equal("set value", run.Trace[0].Verb);
        Assert.Equal("beta", run.Trace[0].ReadBack);

        // And what arrived after it, said on the act's line as well as on the expectation's — so a
        // reader who lands on the verb they wrote is not shown a value the verdict never saw.
        Assert.Equal("BETA", run.Trace[0].Settled);
        Assert.Equal("BETA", run.Trace[1].ReadBack);
    }

    /// <summary>A project whose waits are short, because nothing here is waiting for a red.</summary>
    private ProjectDeclaration Declared()
    {
        var path = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Environment.ProcessPath)}},
              "timeouts": { "resolve": 4000, "act": 4000, "poll": 25 }
            }
            """);

        return ProjectDeclaration.Load(path);
    }
}
