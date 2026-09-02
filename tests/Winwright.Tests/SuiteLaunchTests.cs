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
    public void Two_unshared_cases_cost_one_window_at_a_time_rather_than_two_at_the_end()
    {
        // WW215. Read after the run and before the roll, which is the only moment the two readings
        // differ: StopAll has always left nothing behind, and what was wrong was that it was the
        // first thing to. Every window after the first is another top-level window a locator could
        // match and another candidate for the largest window a process owns.
        var owned = Names();
        var verdict = Suite.Launch(
            [Borrowing("the pane is selected", owned), Borrowing("the pane is still selected", owned)],
            Selection.All,
            register,
            Project());

        Assert.Equal(RunOutcome.Passed, verdict.Outcome);
        Assert.Equal(2, register.Launched.Count);

        Assert.All(register.Launched, one =>
        {
            one.Refresh();
            Assert.True(one.HasExited, $"pid {one.Pid} was still running after the run that owned it");
        });

        // And nothing is reported as having outlived a case, because nothing did.
        Assert.Empty(register.StopAll());
    }

    [Fact]
    public void A_lent_window_is_held_as_long_as_it_is_being_lent_and_not_given_back_early()
    {
        // The exception to the rule above, and the reason it has to be one: the second case reads the
        // window the first one owns, so giving it back after the first case would leave the second
        // borrowing something that has gone.
        var shared = Names(shareable: true);
        var verdict = Suite.Launch(
            [Borrowing("the pane is selected", shared), Borrowing("the pane is still selected", shared)],
            Selection.All,
            register,
            Project(),
            sharing: true);

        Assert.Equal(RunOutcome.Passed, verdict.Outcome);

        var lent = Assert.Single(register.Launched);
        lent.Refresh();
        Assert.False(lent.HasExited, "the lent window was given back while a case was still borrowing it");
        Assert.Single(register.StopAll());
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

    [Fact]
    public void A_capture_with_no_captures_declared_is_refused_before_anything_is_launched()
    {
        // WW348. WW336 answered this as a hole on the run that reached the step, which is the right
        // answer one step too late: a capture in a project with nowhere to put pictures is a fact
        // about the file and the declaration beside it, and both have been read before a window
        // exists. The launch is what the earlier refusal saves, so the launch is what this asserts.
        var capturing = CaseDeclaration.Declared(
            "the names pane is photographed",
            [StepDeclaration.Of("Edit#profileBox", "capture", "the profile box")],
            fixture: Names(),
            catches: "a picture of the pane that stops being a picture of the pane");

        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => Suite.Launch([capturing], Selection.All, register, Project()));

        Assert.Contains("'captures'", refusal.Message, StringComparison.Ordinal);
        Assert.Contains(ProjectDeclaration.FileName, refusal.Message, StringComparison.Ordinal);

        // The whole of what moving it earlier buys, and the one thing a run-time hole cannot claim.
        Assert.Empty(register.Launched);
    }

    [Fact]
    public void A_case_nobody_selected_still_refuses_the_run_it_could_not_have_survived()
    {
        // WW348, and the half that says this is a rule about the file rather than about this run. A
        // selector narrowed to the case that does not capture would otherwise pass while the
        // declaration is still missing for the one beside it — and the next person to widen the
        // selection pays a launch to be told what was knowable now.
        var capturing = CaseDeclaration.Declared(
            "the names pane is photographed",
            [StepDeclaration.Of("Edit#profileBox", "capture", "the profile box")],
            fixture: Names(),
            catches: "a picture of the pane that stops being a picture of the pane");

        var typing = CaseDeclaration.Declared(
            "the profile box takes a name",
            [Typing()],
            fixture: Names(),
            catches: "an editable control the pane draws and no case ever writes to");

        var refusal = Assert.Throws<ScenarioRefusedException>(() => Suite.Launch(
            [typing, capturing],
            Selection.Case("the profile box takes a name"),
            register,
            Project()));

        Assert.Contains("the names pane is photographed", refusal.Message, StringComparison.Ordinal);
        Assert.Empty(register.Launched);
    }

    [Fact]
    public void A_capture_of_an_ordinary_window_is_a_picture_the_application_drew_for_the_run()
    {
        // WW349, end to end and through the door every capture goes through. The route is the
        // off-screen render, which is this block's default and the one the engine cannot take — so
        // the run asks the application, the application draws its own tree into the file the launch
        // told it it may write, and the receipt is composed over what came back.
        var pictures = Path.Combine(root, "pictures");
        var capturing = CaseDeclaration.Declared(
            "the names pane is photographed",
            [StepDeclaration.Of("TabItem#namesPane", "capture", "the names pane")],
            fixture: Names(),
            catches: "a pane that stops drawing what a capture was taken to prove it draws");

        var verdict = Suite.Launch([capturing], Selection.All, register, Capturing(pictures));

        var ran = Assert.Single(verdict.Ran);
        var written = Path.Combine(pictures, "the names pane is photographed", "the names pane.png");

        // The claim, and the reason it is stated as the file rather than as the verdict: a hole is a
        // perfectly good verdict and it is what this answered before. What changed is that there is
        // a picture.
        Assert.True(
            File.Exists(written),
            $"nothing was drawn: {string.Join(" | ", ran.Verdict.Results.Select(one => one.Detail))}");

        Assert.Equal(RunOutcome.Passed, ran.Verdict.Outcome);
        Assert.False(Winwright.Capturing.Colours.In(written).IsFlat);
    }

    private static CaseDeclaration Borrowing(string name, FixtureDeclaration fixture) => CaseDeclaration.Declared(
        name,
        [Reading()],
        fixture: fixture,
        onlyReads: true,
        catches: "a pane whose header stops reporting that it is the selected one");

    /// <summary>
    /// The same project, saying where pictures go. WW349: that directory is also the one the launch
    /// tells the application it may write into, so declaring it is the whole of what an adopter does
    /// to make the default capture route work.
    /// </summary>
    /// <param name="pictures">Where captures are written.</param>
    private ProjectDeclaration Capturing(string pictures)
    {
        var path = Path.Combine(root, "capturing", ProjectDeclaration.FileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "captures": {{System.Text.Json.JsonSerializer.Serialize(pictures)}},
              "timeouts": { "resolve": 4000, "act": 4000, "poll": 25, "launch": 20000 }
            }
            """);

        return ProjectDeclaration.Load(path);
    }

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
