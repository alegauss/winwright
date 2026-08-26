using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW60 and WW62 against a real launch. The fixture is the application under test, started from its
/// own output the way an adopter's would be, so what is proved here is that the declaration reaches
/// the process rather than that the types agree with each other.
/// <para>
/// WW60's claim: the fixture decides what the window contains, and the same declaration is what the
/// expectations were written against — so the identical case passes against one fixture and fails
/// against another, with nothing about the case changing.
/// </para>
/// <para>
/// WW62's claim: a shareable fixture wanted by two read-only cases costs one launch when the
/// invocation asks for sharing, and two when it does not — because a case that still owns its
/// process is what keeps it worth running alone.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class SuiteLaunchTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-launch-").FullName;
    private readonly ProcessRegister register;

    public SuiteLaunchTests() => register = ProcessRegister.For(Project());

    public void Dispose()
    {
        register.Dispose();
        Directory.Delete(root, recursive: true);
    }

    /// <summary>The pane the <c>--names</c> fixture draws, which carries the one editable control.</summary>
    private static FixtureDeclaration Names(bool shareable = false) =>
        FixtureDeclaration.Of("the names pane", arguments: ["--names"], shareable: shareable);

    private static StepDeclaration Typing() =>
        StepDeclaration.Of("Edit#profileBox", "set value", "beta", expected: "beta", reads: "value");

    /// <summary>Selecting the pane that is already selected: an act that leaves the window as found.</summary>
    private static StepDeclaration Reading() =>
        StepDeclaration.Of("TabItem#namesPane", "select", expected: "selected", reads: "selected");

    [Fact]
    public void The_fixture_a_case_declares_is_the_window_its_expectations_are_read_against()
    {
        // WW60. Nothing about the case changes between these two runs. The declaration decides both
        // what was launched and what the expectation describes, and that is the whole claim.
        var against = CaseDeclaration.Declared(
            "the profile box takes a name",
            [Typing()],
            fixture: Names(),
            catches: "an editable control the pane draws and no case ever writes to");

        var passed = Suite.Launch([against], Selection.All, register, Project());

        Assert.Equal(RunOutcome.Passed, passed.Outcome);
        Assert.Equal("the names pane", Assert.Single(passed.Ran).Against.Name);
    }

    [Fact]
    public void The_same_case_against_the_application_as_it_comes_finds_nothing_to_act_on()
    {
        // The other half of the same claim, and the reason the refusal in FixtureDeclaration matters:
        // a case whose fixture did not reach the launch is a case describing a window nobody drew.
        var plain = CaseDeclaration.Declared(
            "the profile box takes a name",
            [Typing()],
            catches: "an editable control the pane draws and no case ever writes to");

        var verdict = Suite.Launch([plain], Selection.All, register, Project());

        Assert.Equal(RunOutcome.Broken, verdict.Outcome);
        Assert.False(Assert.Single(verdict.Ran).Against.Samples);
    }

    [Fact]
    public void A_shareable_fixture_two_read_only_cases_want_costs_one_launch()
    {
        // WW62. Three cases in claude-tray drove the same window and each paid the launch, the first
        // layout pass and the first poll, for a window none of them leaves in a state the next would
        // reject.
        var shared = Names(shareable: true);
        var verdict = Suite.Launch(
            [Borrowing("the pane is selected", shared), Borrowing("the pane is still selected", shared)],
            Selection.All,
            register,
            Project(),
            sharing: true);

        Assert.Equal(RunOutcome.Passed, verdict.Outcome);
        Assert.Single(register.Launched);

        // The first through pays the launch and owns the window; only the second borrows it.
        Assert.Equal([false, true], verdict.Ran.Select(one => one.Lent));
        Assert.Contains("on a borrowed window", verdict.Ran[1].ToString());
    }

    [Fact]
    public void The_same_two_cases_own_their_processes_where_the_invocation_did_not_ask_to_share()
    {
        // Not a default, and this is why: the property that keeps a case worth running alone is that
        // running it alone is what it does.
        var shared = Names(shareable: true);
        var verdict = Suite.Launch(
            [Borrowing("the pane is selected", shared), Borrowing("the pane is still selected", shared)],
            Selection.All,
            register,
            Project());

        Assert.Equal(RunOutcome.Passed, verdict.Outcome);
        Assert.Equal(2, register.Launched.Count);
        Assert.All(verdict.Ran, one => Assert.False(one.Lent));
    }

    [Fact]
    public void A_case_that_does_not_only_read_is_never_lent_a_window_however_shareable_the_fixture()
    {
        // One acting case in the group is a case that hands the next one whatever it left behind,
        // and the red that produces is about the order the run happened to walk them in.
        var shared = Names(shareable: true);
        var verdict = Suite.Launch(
            [
                Borrowing("the pane is selected", shared),
                CaseDeclaration.Declared(
                    "the profile box takes a name", [Typing()], fixture: shared, catches: "an unwritten control"),
            ],
            Selection.All,
            register,
            Project(),
            sharing: true);

        Assert.Equal(RunOutcome.Passed, verdict.Outcome);
        Assert.Equal(2, register.Launched.Count);
        Assert.All(verdict.Ran, one => Assert.False(one.Lent));
    }

    [Fact]
    public void A_fixture_that_was_never_declared_shareable_is_not_lent_either()
    {
        var owned = Names();
        var verdict = Suite.Launch(
            [Borrowing("the pane is selected", owned), Borrowing("the pane is still selected", owned)],
            Selection.All,
            register,
            Project(),
            sharing: true);

        Assert.Equal(RunOutcome.Passed, verdict.Outcome);
        Assert.Equal(2, register.Launched.Count);
    }

    [Fact]
    public void One_case_of_a_shared_fixture_run_alone_still_owns_its_process()
    {
        // The sentence WW62's design ends on, asserted rather than argued.
        var shared = Names(shareable: true);
        var verdict = Suite.Launch(
            [Borrowing("the pane is selected", shared), Borrowing("the pane is still selected", shared)],
            Selection.Case("the pane is selected"),
            register,
            Project(),
            sharing: true);

        Assert.Single(register.Launched);
        Assert.False(Assert.Single(verdict.Ran).Lent);
        Assert.Single(verdict.Skipped);
    }

    [Fact]
    public void A_case_lent_a_window_it_never_said_it_only_reads_is_refused_rather_than_run()
    {
        // The guard sits on the run and not only on the grouping: whatever decides to lend a window,
        // a case that does not say it only reads cannot be handed one.
        var acting = CaseDeclaration.Declared(
            "the profile box takes a name", [Typing()], fixture: Names(shareable: true), catches: "an unwritten control");

        var launched = register.Launch(Names().Starting(Project().Executable));
        var window = Winwright.Windowing.TopLevelWindows.Largest(launched.Pid);
        Assert.True(
            Winwright.Locating.Attempt.UntilTrue(
                () => Winwright.Windowing.TopLevelWindows.Largest(launched.Pid) is not null, 15000, 50).Happened,
            "the fixture drew no window");

        window = Winwright.Windowing.TopLevelWindows.Largest(launched.Pid);
        var refusal = Assert.Throws<ScenarioRefusedException>(() => CaseRun.Of(
            acting,
            System.Windows.Automation.AutomationElement.FromHandle(window!.Handle),
            Project(),
            lent: true));

        Assert.Contains("does not say it only reads", refusal.Because);
    }

    private static CaseDeclaration Borrowing(string name, FixtureDeclaration fixture) => CaseDeclaration.Declared(
        name,
        [Reading()],
        fixture: fixture,
        onlyReads: true,
        catches: "a pane whose header stops reporting that it is the selected one");

    /// <summary>A project whose executable is the built fixture, which is the application under test.</summary>
    private ProjectDeclaration Project()
    {
        var path = Path.Combine(root, ProjectDeclaration.FileName);
        if (!File.Exists(path))
        {
            File.WriteAllText(
                path,
                $$"""
                {
                  "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
                  "timeouts": { "resolve": 4000, "act": 4000, "poll": 25, "launch": 20000 }
                }
                """);
        }

        return ProjectDeclaration.Load(path);
    }
}
